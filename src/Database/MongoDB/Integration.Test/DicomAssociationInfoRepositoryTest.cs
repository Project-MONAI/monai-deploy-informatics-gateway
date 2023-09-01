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
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test;
using Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories;
using MongoDB.Driver;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Integration.Test
{
    [Collection("MongoDatabase")]
    public class DicomAssociationInfoRepositoryTest
    {
        private readonly MongoDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<DicomAssociationInfoRepository>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public DicomAssociationInfoRepositoryTest(MongoDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _databaseFixture.InitDatabaseWithDicomAssociationInfoEntries();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<DicomAssociationInfoRepository>>();
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
        public async Task GivenADicomAssociationInfo_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var association = new DicomAssociationInfo { CalledAeTitle = "called", CallingAeTitle = "calling", CorrelationId = Guid.NewGuid().ToString(), DateTimeCreated = DateTime.UtcNow, RemoteHost = "host", RemotePort = 100 };
            association.FileReceived();
            association.FileReceived();
            association.FileReceived();
            association.Disconnect();

            var store = new DicomAssociationInfoRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
            await store.AddAsync(association).ConfigureAwait(false);

            var collection = _databaseFixture.Database.GetCollection<DicomAssociationInfo>(nameof(DicomAssociationInfo));
            var actual = await collection.Find(p => p.Id == association.Id).FirstOrDefaultAsync().ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(association.FileCount, actual!.FileCount);
            Assert.Equal(association.Duration, actual!.Duration);
            Assert.Equal(association.CalledAeTitle, actual!.CalledAeTitle);
            Assert.Equal(association.CallingAeTitle, actual!.CallingAeTitle);
            Assert.Equal(association.CorrelationId, actual!.CorrelationId);

            actual!.DateTimeCreated.Should().BeCloseTo(association.DateTimeCreated, TimeSpan.FromMilliseconds(500));
            actual!.DateTimeDisconnected.Should().BeCloseTo(association.DateTimeDisconnected, TimeSpan.FromMilliseconds(500));
        }

        [Fact]
        public async Task GivenDestinationApplicationEntitiesInTheDatabase_WhenToListIsCalled_ExpectAllEntitiesToBeReturned()
        {
            var store = new DicomAssociationInfoRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);

            var collection = _databaseFixture.Database.GetCollection<DicomAssociationInfo>(nameof(DicomAssociationInfo));
            var expected = await collection.Find(Builders<DicomAssociationInfo>.Filter.Empty).ToListAsync().ConfigureAwait(false);
            var actual = await store.ToListAsync().ConfigureAwait(false);

            actual.Should().BeEquivalentTo(expected, options => options.Excluding(p => p.DateTimeCreated));
        }
    }
}
