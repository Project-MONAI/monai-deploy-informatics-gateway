﻿/*
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
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test;
using Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories;
using MongoDB.Driver;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Integration.Test
{
    [Collection("MongoDatabase")]
    public class DestinationApplicationEntityRepositoryTest
    {
        private readonly MongoDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<DestinationApplicationEntityRepository>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public DestinationApplicationEntityRepositoryTest(MongoDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _databaseFixture.InitDatabaseWithDestinationApplicationEntities();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<DestinationApplicationEntityRepository>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => databaseFixture.Client);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Database.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public async Task GivenADestinationApplicationEntity_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var aet = new DestinationApplicationEntity { AeTitle = "AET", HostIp = "1.2.3.4", Port = 114, Name = "AET" };

            var store = new DestinationApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
            await store.AddAsync(aet).ConfigureAwait(false);

            var collection = _databaseFixture.Database.GetCollection<DestinationApplicationEntity>(nameof(DestinationApplicationEntity));
            var actual = await collection.Find(p => p.Name == aet.Name).FirstOrDefaultAsync().ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(aet.AeTitle, actual!.AeTitle);
            Assert.Equal(aet.HostIp, actual!.HostIp);
            Assert.Equal(aet.Port, actual!.Port);
            Assert.Equal(aet.Name, actual!.Name);
        }

        [Fact]
        public async Task GivenAExpressionFilter_WhenContainsAsyncIsCalled_ExpectItToReturnMatchingObjects()
        {
            var store = new DestinationApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);

            var result = await store.ContainsAsync(p => p.AeTitle == "AET1").ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.AeTitle.Equals("AET1")).ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.AeTitle != "AET1" && p.Port == 114 && p.HostIp == "1.2.3.4").ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.Port == 114 && p.HostIp == "1.2.3.4").ConfigureAwait(false);
            Assert.True(result);
            result = await store.ContainsAsync(p => p.Port == 999).ConfigureAwait(false);
            Assert.False(result);
        }

        [Fact]
        public async Task GivenAAETitleName_WhenFindByNameAsyncIsCalled_ExpectItToReturnMatchingEntity()
        {
            var store = new DestinationApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);

            var actual = await store.FindByNameAsync("AET1").ConfigureAwait(false);
            Assert.NotNull(actual);
            Assert.Equal("AET1", actual!.AeTitle);
            Assert.Equal("1.2.3.4", actual!.HostIp);
            Assert.Equal(114, actual!.Port);
            Assert.Equal("AET1", actual!.Name);

            actual = await store.FindByNameAsync("AET6").ConfigureAwait(false);
            Assert.Null(actual);
        }

        [Fact]
        public async Task GivenADestinationApplicationEntity_WhenRemoveIsCalled_ExpectItToDeleted()
        {
            var store = new DestinationApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);

            var expected = await store.FindByNameAsync("AET5").ConfigureAwait(false);
            Assert.NotNull(expected);

            var actual = await store.RemoveAsync(expected!).ConfigureAwait(false);
            Assert.Same(expected, actual);

            var collection = _databaseFixture.Database.GetCollection<DestinationApplicationEntity>(nameof(DestinationApplicationEntity));
            var dbResult = await collection.Find(p => p.Name == "AET5").FirstOrDefaultAsync().ConfigureAwait(false);
            Assert.Null(dbResult);
        }

        [Fact]
        public async Task GivenDestinationApplicationEntitiesInTheDatabase_WhenToListIsCalled_ExpectAllEntitiesToBeReturned()
        {
            var store = new DestinationApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);

            var collection = _databaseFixture.Database.GetCollection<DestinationApplicationEntity>(nameof(DestinationApplicationEntity));
            var expected = await collection.Find(Builders<DestinationApplicationEntity>.Filter.Empty).ToListAsync().ConfigureAwait(false);
            var actual = await store.ToListAsync().ConfigureAwait(false);

            actual.Should().BeEquivalentTo(expected, options => options.Excluding(p => p.DateTimeCreated));
        }

        [Fact]
        public async Task GivenADestinationApplicationEntity_WhenUpdatedIsCalled_ExpectItToSaved()
        {
            var store = new DestinationApplicationEntityRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);

            var expected = await store.FindByNameAsync("AET3").ConfigureAwait(false);
            Assert.NotNull(expected);

            expected!.AeTitle = "AET100";
            expected!.Port = 1000;
            expected!.HostIp = "loalhost";

            var actual = await store.UpdateAsync(expected).ConfigureAwait(false);
            Assert.Equal(expected, actual);

            var dbResult = await store.FindByNameAsync("AET3").ConfigureAwait(false);
            Assert.NotNull(dbResult);
            Assert.Equal(expected.AeTitle, dbResult!.AeTitle);
            Assert.Equal(expected.HostIp, dbResult!.HostIp);
            Assert.Equal(expected.Port, dbResult!.Port);
        }
    }
}
