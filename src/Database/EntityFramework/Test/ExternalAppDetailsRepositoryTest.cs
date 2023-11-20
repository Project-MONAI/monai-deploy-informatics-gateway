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
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories;
using Moq;


namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test
{
    [Collection("SqliteDatabase")]
    public class ExternalAppDetailsRepositoryTest
    {
        private readonly SqliteDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<ExternalAppDetailsRepository>> _logger;
        private readonly IOptions<DatabaseOptions> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public ExternalAppDetailsRepositoryTest(SqliteDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _databaseFixture.InitDatabaseWithExternalAppDetailsEntries();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<ExternalAppDetailsRepository>>();
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
        public async Task GivenDestinationExternalAppInTheDatabase_WhenGetAsyncCalled_ExpectEntitieToBeReturned()
        {
            var store = new ExternalAppDetailsRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            var startTime = DateTime.Now;
            var endTime = DateTime.MinValue;

            var expected = _databaseFixture.DatabaseContext.Set<ExternalAppDetails>()
                .Where(t => t.StudyInstanceUid == "1");
            var actual = await store.GetAsync("1").ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GivenDestinationExternalAppInTheDatabase_WhenGetAsyncCalled_ExpectEntitieToBeReturned2()
        {
            var store = new ExternalAppDetailsRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            var startTime = DateTime.Now;
            var endTime = DateTime.MinValue;

            var expected = _databaseFixture.DatabaseContext.Set<ExternalAppDetails>()
                .Where(t => t.PatientIdOutBound == "2")
                .Take(1).First();
            var actual = await store.GetByPatientIdOutboundAsync("2", new CancellationToken()).ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GivenDestinationExternalAppInTheDatabase_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var association = new ExternalAppDetails
            {
                StudyInstanceUid = "3",
                WorkflowInstanceId = "calling",
                CorrelationId = Guid.NewGuid().ToString(),
                DateTimeCreated = DateTime.UtcNow,
                ExportTaskID = "host"
            };

            var store = new ExternalAppDetailsRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(association).ConfigureAwait(false);
            var actual = await _databaseFixture.DatabaseContext.Set<ExternalAppDetails>().FirstOrDefaultAsync(p => p.StudyInstanceUid.Equals(association.StudyInstanceUid)).ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(association.DateTimeCreated, actual!.DateTimeCreated);
            Assert.Equal(association.WorkflowInstanceId, actual!.WorkflowInstanceId);
            Assert.Equal(association.ExportTaskID, actual!.ExportTaskID);
            Assert.Equal(association.CorrelationId, actual!.CorrelationId);
        }
    }
}
