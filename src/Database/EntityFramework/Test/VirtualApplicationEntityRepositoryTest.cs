/*
 * Copyright 2023 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test
{
    [Collection("SqliteDatabase")]
    public class VirtualApplicationEntityRepositoryTest
    {
        private readonly SqliteDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<VirtualApplicationEntityRepository>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public VirtualApplicationEntityRepositoryTest(SqliteDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _databaseFixture.InitDatabaseWithVirtualApplicationEntities();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<VirtualApplicationEntityRepository>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => databaseFixture.DatabaseContext);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Database.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public async Task GivenAVirtualApplicationEntity_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var aet = new VirtualApplicationEntity
            {
                VirtualAeTitle = "AET",
                Name = "AET",
                Workflows = new List<string> { "W1", "W2" },
                PluginAssemblies = new List<string> { "AssemblyA", "AssemblyB", "AssemblyC" },
            };

            var store = new VirtualApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(aet).ConfigureAwait(false);
            var actual = await _databaseFixture.DatabaseContext.Set<VirtualApplicationEntity>().FirstOrDefaultAsync(p => p.Name.Equals(aet.Name)).ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(aet.VirtualAeTitle, actual!.VirtualAeTitle);
            Assert.Equal(aet.Name, actual!.Name);
            Assert.Equal(aet.Workflows, actual!.Workflows);
            Assert.Equal(aet.PluginAssemblies, actual!.PluginAssemblies);
        }

        [Fact]
        public async Task GivenAExpressionFilter_WhenContainsAsyncIsCalled_ExpectItToReturnMatchingObjects()
        {
            var store = new VirtualApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var result = await store.ContainsAsync(p => p.VirtualAeTitle == "AET1").ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.VirtualAeTitle.Equals("AET1", StringComparison.Ordinal)).ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.Name != "AET2").ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.Name == "AET6").ConfigureAwait(false);
            Assert.False(result);
        }

        [Fact]
        public async Task GivenAAETitleName_WhenFindByNameAsyncIsCalled_ExpectItToReturnMatchingEntity()
        {
            var store = new VirtualApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var actual = await store.FindByNameAsync("AET1").ConfigureAwait(false);
            Assert.NotNull(actual);
            Assert.Equal("AET1", actual!.VirtualAeTitle);
            Assert.Equal("AET1", actual!.Name);

            actual = await store.FindByNameAsync("AET6").ConfigureAwait(false);
            Assert.Null(actual);
        }

        [Fact]
        public async Task GivenAAETitleName_WhenFindByAeTitleAsyncIsCalled_ExpectItToReturnMatchingEntity()
        {
            var store = new VirtualApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var actual = await store.FindByAeTitleAsync("AET1").ConfigureAwait(false);
            Assert.NotNull(actual);
            Assert.Equal("AET1", actual!.VirtualAeTitle);
            Assert.Equal("AET1", actual!.Name);

            actual = await store.FindByAeTitleAsync("AET6").ConfigureAwait(false);
            Assert.Null(actual);
        }

        [Fact]
        public async Task GivenAVirtualApplicationEntity_WhenRemoveIsCalled_ExpectItToDeleted()
        {
            var store = new VirtualApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = await store.FindByAeTitleAsync("AET5").ConfigureAwait(false);
            Assert.NotNull(expected);

            var actual = await store.RemoveAsync(expected!).ConfigureAwait(false);
            Assert.Same(expected, actual);

            var dbResult = await _databaseFixture.DatabaseContext.Set<VirtualApplicationEntity>().FirstOrDefaultAsync(p => p.Name == "AET5").ConfigureAwait(false);
            Assert.Null(dbResult);
        }

        [Fact]
        public async Task GivenVirtualApplicationEntitiesInTheDatabase_WhenToListIsCalled_ExpectAllEntitiesToBeReturned()
        {
            var store = new VirtualApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = await _databaseFixture.DatabaseContext.Set<VirtualApplicationEntity>().ToListAsync().ConfigureAwait(false);
            var actual = await store.ToListAsync().ConfigureAwait(false);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GivenAVirtualApplicationEntity_WhenUpdatedIsCalled_ExpectItToSaved()
        {
            var store = new VirtualApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = await store.FindByAeTitleAsync("AET3").ConfigureAwait(false);
            Assert.NotNull(expected);

            expected!.VirtualAeTitle = "AET100";

            var actual = await store.UpdateAsync(expected).ConfigureAwait(false);
            Assert.Equal(expected, actual);

            var dbResult = await store.FindByAeTitleAsync("AET100").ConfigureAwait(false);
            Assert.NotNull(dbResult);
            Assert.Equal(expected.VirtualAeTitle, dbResult!.VirtualAeTitle);
        }
    }
}
