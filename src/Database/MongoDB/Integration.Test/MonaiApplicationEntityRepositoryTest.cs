/*
 * Copyright 2022 MONAI Consortium
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

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test;
using Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories;
using MongoDB.Driver;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Integration.Test
{
    [Collection("MongoDatabase")]
    public class MonaiApplicationEntityRepositoryTest
    {
        private readonly MongoDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<MonaiApplicationEntityRepository>> _logger;
        private readonly IOptions<DatabaseOptions> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public MonaiApplicationEntityRepositoryTest(MongoDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _databaseFixture.InitDatabaseWithMonaiApplicationEntities();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<MonaiApplicationEntityRepository>>();
            _options = _databaseFixture.Options;

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => databaseFixture.Client);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public async Task GivenAMonaiApplicationEntity_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var aet = new MonaiApplicationEntity
            {
                AeTitle = "AET",
                Name = "AET",
                Timeout = 100,
                AllowedSopClasses = new List<string> { "1", "2", "3" },
                Workflows = new List<string> { "W1", "W2" },
                Grouping = "G",
                IgnoredSopClasses = new List<string> { "4", "5" }
            };

            var store = new MonaiApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(aet).ConfigureAwait(false);

            var collection = _databaseFixture.Database.GetCollection<MonaiApplicationEntity>(nameof(MonaiApplicationEntity));
            var actual = await collection.Find(p => p.Name == aet.Name).FirstOrDefaultAsync().ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(aet.AeTitle, actual!.AeTitle);
            Assert.Equal(aet.Name, actual!.Name);
            Assert.Equal(aet.Timeout, actual!.Timeout);
            Assert.Equal(aet.AllowedSopClasses, actual!.AllowedSopClasses);
            Assert.Equal(aet.Workflows, actual!.Workflows);
            Assert.Equal(aet.Grouping, actual!.Grouping);
            Assert.Equal(aet.IgnoredSopClasses, actual!.IgnoredSopClasses);
        }

        [Fact]
        public async Task GivenAExpressionFilter_WhenContainsAsyncIsCalled_ExpectItToReturnMatchingObjects()
        {
            var store = new MonaiApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var result = await store.ContainsAsync(p => p.AeTitle == "AET1").ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.AeTitle.Equals("AET1")).ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.Name != "AET2").ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.Name == "AET6").ConfigureAwait(false);
            Assert.False(result);
        }

        [Fact]
        public async Task GivenAAETitleName_WhenFindByNameAsyncIsCalled_ExpectItToReturnMatchingEntity()
        {
            var store = new MonaiApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var actual = await store.FindByNameAsync("AET1").ConfigureAwait(false);
            Assert.NotNull(actual);
            Assert.Equal("AET1", actual!.AeTitle);
            Assert.Equal("AET1", actual!.Name);

            actual = await store.FindByNameAsync("AET6").ConfigureAwait(false);
            Assert.Null(actual);
        }

        [Fact]
        public async Task GivenAMonaiApplicationEntity_WhenRemoveIsCalled_ExpectItToDeleted()
        {
            var store = new MonaiApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = await store.FindByNameAsync("AET5").ConfigureAwait(false);
            Assert.NotNull(expected);

            var actual = await store.RemoveAsync(expected!).ConfigureAwait(false);
            Assert.Same(expected, actual);

            var collection = _databaseFixture.Database.GetCollection<MonaiApplicationEntity>(nameof(MonaiApplicationEntity));
            var dbResult = await collection.Find(p => p.Name == "AET5").FirstOrDefaultAsync().ConfigureAwait(false);
            Assert.Null(dbResult);
        }

        [Fact]
        public async Task GivenDestinationApplicationEntitiesInTheDatabase_WhenToListIsCalled_ExpectAllEntitiesToBeReturned()
        {
            var store = new MonaiApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var collection = _databaseFixture.Database.GetCollection<MonaiApplicationEntity>(nameof(MonaiApplicationEntity));
            var expected = await collection.Find(Builders<MonaiApplicationEntity>.Filter.Empty).ToListAsync().ConfigureAwait(false);
            var actual = await store.ToListAsync().ConfigureAwait(false);

            actual.Should().BeEquivalentTo(expected, options => options.Excluding(p => p.DateTimeCreated));
        }

        [Fact]
        public async Task GivenAMonaiApplicationEntity_WhenUpdatedIsCalled_ExpectItToSaved()
        {
            var store = new MonaiApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = await store.FindByNameAsync("AET3").ConfigureAwait(false);
            Assert.NotNull(expected);

            expected!.AeTitle = "AET100";

            var actual = await store.UpdateAsync(expected).ConfigureAwait(false);
            Assert.Equal(expected, actual);

            var dbResult = await store.FindByNameAsync("AET3").ConfigureAwait(false);
            Assert.NotNull(dbResult);
            Assert.Equal(expected.AeTitle, dbResult!.AeTitle);
        }
    }
}
