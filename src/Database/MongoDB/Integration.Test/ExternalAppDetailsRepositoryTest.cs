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
    public class ExternalAppDetailsRepositoryTest
    {
        private readonly MongoDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<ExternalAppDetailsRepository>> _logger;
        private readonly IOptions<DatabaseOptions> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public ExternalAppDetailsRepositoryTest(MongoDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _databaseFixture.InitDatabaseWithExternalAppEntities();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<ExternalAppDetailsRepository>>();
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
        public async Task GivenExternalAppDetailsEntitiesInTheDatabase_WhenGetAsyncCalled_ExpectEntitieToBeReturned()
        {
            var store = new ExternalAppDetailsRepository(_serviceScopeFactory.Object, _logger.Object, _databaseFixture.Options);

            var collection = _databaseFixture.Database.GetCollection<ExternalAppDetails>(nameof(ExternalAppDetails));

            var expected = (await collection.FindAsync(e => e.StudyInstanceUid == "2").ConfigureAwait(false)).First();
            var actual = (await store.GetAsync("2", new CancellationToken()).ConfigureAwait(false)).FirstOrDefault();

            actual.Should().NotBeNull();
            Assert.Equal(expected.StudyInstanceUid, actual!.StudyInstanceUid);
            Assert.Equal(expected.ExportTaskID, actual!.ExportTaskID);
            Assert.Equal(expected.CorrelationId, actual!.CorrelationId);
            Assert.Equal(expected.WorkflowInstanceId, actual!.WorkflowInstanceId);
        }

        [Fact]
        public async Task GivenAExternalAppDetails_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var app = new ExternalAppDetails { StudyInstanceUid = "3", ExportTaskID = "ExportTaskID3", CorrelationId = "CorrelationId3", WorkflowInstanceId = "WorkflowInstanceId3" };

            var store = new ExternalAppDetailsRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(app, new CancellationToken()).ConfigureAwait(false);

            var collection = _databaseFixture.Database.GetCollection<ExternalAppDetails>(nameof(ExternalAppDetails));
            var actual = await collection.Find(p => p.Id == app.Id).FirstOrDefaultAsync().ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(app.StudyInstanceUid, actual!.StudyInstanceUid);
            Assert.Equal(app.ExportTaskID, actual!.ExportTaskID);
            Assert.Equal(app.CorrelationId, actual!.CorrelationId);
            Assert.Equal(app.WorkflowInstanceId, actual!.WorkflowInstanceId);

            actual!.DateTimeCreated.Should().BeCloseTo(app.DateTimeCreated, TimeSpan.FromMilliseconds(500));
        }

        [Fact]
        public async Task GivenExternalAppDetailsEntitiesInTheDatabase_WhenGetPatientOutboundAsyncCalled_ExpectEntitieToBeReturned()
        {
            var store = new ExternalAppDetailsRepository(_serviceScopeFactory.Object, _logger.Object, _databaseFixture.Options);

            var collection = _databaseFixture.Database.GetCollection<ExternalAppDetails>(nameof(ExternalAppDetails));

            var expected = (await collection.FindAsync(e => e.PatientIdOutBound == "pat1out1").ConfigureAwait(false)).First();
            var actual = (await store.GetByPatientIdOutboundAsync("pat1out1", new CancellationToken()).ConfigureAwait(false));

            actual.Should().NotBeNull();
            Assert.Equal(expected.StudyInstanceUid, actual!.StudyInstanceUid);
            Assert.Equal(expected.ExportTaskID, actual!.ExportTaskID);
            Assert.Equal(expected.CorrelationId, actual!.CorrelationId);
            Assert.Equal(expected.WorkflowInstanceId, actual!.WorkflowInstanceId);
        }

        [Fact]
        public async Task GivenExternalAppDetailsEntitiesInTheDatabase_WhenGetStudyIdOutboundAsyncCalled_ExpectEntitieToBeReturned()
        {
            var store = new ExternalAppDetailsRepository(_serviceScopeFactory.Object, _logger.Object, _databaseFixture.Options);

            var collection = _databaseFixture.Database.GetCollection<ExternalAppDetails>(nameof(ExternalAppDetails));

            var expected = (await collection.FindAsync(e => e.StudyInstanceUidOutBound == "sudIdOut2").ConfigureAwait(false)).First();
            var actual = (await store.GetByStudyIdOutboundAsync("sudIdOut2", new CancellationToken()).ConfigureAwait(false));

            actual.Should().NotBeNull();
            Assert.Equal(expected.StudyInstanceUid, actual!.StudyInstanceUid);
            Assert.Equal(expected.ExportTaskID, actual!.ExportTaskID);
            Assert.Equal(expected.CorrelationId, actual!.CorrelationId);
            Assert.Equal(expected.WorkflowInstanceId, actual!.WorkflowInstanceId);
        }
    }
}
