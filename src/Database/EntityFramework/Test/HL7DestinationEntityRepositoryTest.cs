/*
 * Copyright 2022-2023 MONAI Consortium
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
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test
{
    [Collection("SqliteDatabase")]
    public class HL7DestinationEntityRepositoryTest
    {
        private readonly SqliteDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<HL7DestinationEntityRepository>> _logger;
        private readonly IOptions<DatabaseOptions> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public HL7DestinationEntityRepositoryTest(SqliteDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _databaseFixture.InitDatabaseWithHL7DestinationEntities();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<HL7DestinationEntityRepository>>();
            _options = Options.Create(new DatabaseOptions());

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => databaseFixture.DatabaseContext);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Retries.DelaysMilliseconds = [1, 1, 1];
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public async Task GivenAHL7DestinationEntity_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var aet = new HL7DestinationEntity { HostIp = "1.2.3.4", Port = 114, Name = "AET" };

            var store = new HL7DestinationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(aet).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            var actual = await _databaseFixture.DatabaseContext.Set<HL7DestinationEntity>().FirstOrDefaultAsync(p => p.Name.Equals(aet.Name)).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            Assert.NotNull(actual);
            Assert.Equal(aet.HostIp, actual!.HostIp);
            Assert.Equal(aet.Port, actual!.Port);
            Assert.Equal(aet.Name, actual!.Name);
        }

        [Fact]
        public async Task GivenAExpressionFilter_WhenContainsAsyncIsCalled_ExpectItToReturnMatchingObjects()
        {
            var store = new HL7DestinationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            var result = await store.ContainsAsync(p => p.Port == 114 && p.HostIp == "1.2.3.4").ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.Port == 114 && p.HostIp == "1.2.3.4").ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.Port == 999).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.False(result);
        }

        [Fact]
        public async Task GivenAAETitleName_WhenFindByNameAsyncIsCalled_ExpectItToReturnMatchingEntity()
        {
            var store = new HL7DestinationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var actual = await store.FindByNameAsync("AET1").ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(actual);
            Assert.Equal("1.2.3.4", actual!.HostIp);
            Assert.Equal(114, actual!.Port);
            Assert.Equal("AET1", actual!.Name);

            actual = await store.FindByNameAsync("AET6").ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Null(actual);
        }

        [Fact]
        public async Task GivenAHL7DestinationEntity_WhenRemoveIsCalled_ExpectItToDeleted()
        {
            var store = new HL7DestinationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = await store.FindByNameAsync("AET5").ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(expected);

            var actual = await store.RemoveAsync(expected!).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Same(expected, actual);

            var dbResult = await _databaseFixture.DatabaseContext.Set<HL7DestinationEntity>().FirstOrDefaultAsync(p => p.Name == "AET5").ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Null(dbResult);
        }

        [Fact]
        public async Task GivenHL7DestinationEntitiesInTheDatabase_WhenToListIsCalled_ExpectAllEntitiesToBeReturned()
        {
            var store = new HL7DestinationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = await _databaseFixture.DatabaseContext.Set<HL7DestinationEntity>().ToListAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            var actual = await store.ToListAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GivenAHL7DestinationEntity_WhenUpdatedIsCalled_ExpectItToSaved()
        {
            var store = new HL7DestinationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = await store.FindByNameAsync("AET3").ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(expected);

            expected!.Port = 1000;
            expected!.HostIp = "loalhost";

            var actual = await store.UpdateAsync(expected).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Equal(expected, actual);

            var dbResult = await store.FindByNameAsync("AET3").ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(dbResult);
            Assert.Equal(expected.HostIp, dbResult!.HostIp);
            Assert.Equal(expected.Port, dbResult!.Port);
        }
    }
}
