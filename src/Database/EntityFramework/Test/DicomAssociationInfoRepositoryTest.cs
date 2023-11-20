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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test
{
    [Collection("SqliteDatabase")]
    public class DicomAssociationInfoRepositoryTest
    {
        private readonly SqliteDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<DicomAssociationInfoRepository>> _logger;
        private readonly IOptions<DatabaseOptions> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public DicomAssociationInfoRepositoryTest(SqliteDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _databaseFixture.InitDatabaseWithDicomAssociationInfoEntries();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<DicomAssociationInfoRepository>>();
            _options = Options.Create(new DatabaseOptions());

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => databaseFixture.DatabaseContext);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public async Task GivenDestinationApplicationEntitiesInTheDatabase_WhenGetAllAsyncCalled_ExpectLimitedEntitiesToBeReturned()
        {
            var store = new DicomAssociationInfoRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            var startTime = DateTime.Now;
            var endTime = DateTime.MinValue;
            var filter = new Func<DicomAssociationInfo, bool>(t =>
                t.DateTimeDisconnected >= startTime.ToUniversalTime() &&
                t.DateTimeDisconnected <= endTime.ToUniversalTime());

            var expected = _databaseFixture.DatabaseContext.Set<DicomAssociationInfo>()
                .Where(filter)
                .Skip(0)
                .Take(1)
                .ToList();
            var actual = await store.GetAllAsync(0, 1, startTime, endTime, default).ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GivenADicomAssociationInfo_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var association = new DicomAssociationInfo { CalledAeTitle = "called", CallingAeTitle = "calling", CorrelationId = Guid.NewGuid().ToString(), DateTimeCreated = DateTime.UtcNow, RemoteHost = "host", RemotePort = 100 };
            association.FileReceived(Guid.NewGuid().ToString());
            association.FileReceived(Guid.NewGuid().ToString());
            association.FileReceived(Guid.NewGuid().ToString());
            association.FileReceived(null);
            association.FileReceived(string.Empty);
            association.Disconnect();
            association.Disconnect();

            var store = new DicomAssociationInfoRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(association).ConfigureAwait(false);
            var actual = await _databaseFixture.DatabaseContext.Set<DicomAssociationInfo>().FirstOrDefaultAsync(p => p.Id.Equals(association.Id)).ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(association.DateTimeCreated, actual!.DateTimeCreated);
            Assert.Equal(association.DateTimeDisconnected, actual!.DateTimeDisconnected);
            Assert.Equal(3, actual!.FileCount);
            Assert.Equal(association.FileCount, actual!.FileCount);
            Assert.Equal(association.Duration, actual!.Duration);
            Assert.Equal(association.CalledAeTitle, actual!.CalledAeTitle);
            Assert.Equal(association.CallingAeTitle, actual!.CallingAeTitle);
            Assert.Equal(association.CorrelationId, actual!.CorrelationId);
        }

        [Fact]
        public async Task GivenDestinationApplicationEntitiesInTheDatabase_WhenToListIsCalled_ExpectAllEntitiesToBeReturned()
        {
            var store = new DicomAssociationInfoRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = await _databaseFixture.DatabaseContext.Set<DicomAssociationInfo>().ToListAsync().ConfigureAwait(false);
            var actual = await store.ToListAsync().ConfigureAwait(false);

            Assert.Equal(expected, actual);
        }
    }
}
