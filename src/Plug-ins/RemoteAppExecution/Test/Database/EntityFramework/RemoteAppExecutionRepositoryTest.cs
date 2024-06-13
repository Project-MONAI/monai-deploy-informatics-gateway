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

using FellowOakDicom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database.EntityFramework;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Test.Database.EntityFramework
{
    [Collection("SqliteDatabase")]
    public class RemoteAppExecutionRepositoryTest
    {
        private readonly SqliteDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<RemoteAppExecutionRepository>> _logger;
        private readonly IOptions<DatabaseOptions> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public RemoteAppExecutionRepositoryTest(SqliteDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _databaseFixture.InitDatabaseWithRemoteAppExecutions();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<RemoteAppExecutionRepository>>();
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
        public async Task GivenARemoteAppExecution_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var record = new RemoteAppExecution
            {
                CorrelationId = Guid.NewGuid().ToString(),
                ExportTaskId = Guid.NewGuid().ToString(),
                Id = Guid.NewGuid(),
                RequestTime = DateTimeOffset.UtcNow,
                StudyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                SeriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                SopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
            };

            record.OriginalValues.Add(DicomTag.StudyInstanceUID.ToString(), DicomUIDGenerator.GenerateDerivedFromUUID().UID);
            record.OriginalValues.Add(DicomTag.SeriesInstanceUID.ToString(), DicomUIDGenerator.GenerateDerivedFromUUID().UID);
            record.OriginalValues.Add(DicomTag.SOPInstanceUID.ToString(), DicomUIDGenerator.GenerateDerivedFromUUID().UID);
            record.OriginalValues.Add(DicomTag.PatientID.ToString(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16));
            record.OriginalValues.Add(DicomTag.AccessionNumber.ToString(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16));
            record.OriginalValues.Add(DicomTag.StudyDescription.ToString(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16));

            var store = new RemoteAppExecutionRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(record).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            var actual = await _databaseFixture.DatabaseContext.Set<RemoteAppExecution>().FirstOrDefaultAsync(p => p.Id.Equals(record.Id)).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            Assert.NotNull(actual);
            Assert.Equal(record.CorrelationId, actual!.CorrelationId);
            Assert.Equal(record.ExportTaskId, actual!.ExportTaskId);
            Assert.Equal(record.Id, actual!.Id);
            Assert.Equal(record.RequestTime, actual!.RequestTime);
            Assert.Equal(record.OriginalValues, actual.OriginalValues);
        }

        [Fact]
        public async Task GivenARemoteAppExecution_WhenRemoveIsCalled_ExpectItToDeleted()
        {
            var store = new RemoteAppExecutionRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var record = _databaseFixture.RemoteAppExecutions.First();
            var expected = await store.GetAsync(record.SopInstanceUid).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(expected);

            var actual = await store.RemoveAsync(expected!).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Same(expected, actual);

            var dbResult = await _databaseFixture.DatabaseContext.Set<RemoteAppExecution>().FirstOrDefaultAsync(p => p.Id == record.Id).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Null(dbResult);
        }

        [Fact]
        public async Task GivenARemoteAppExecution_WhenGetAsyncIsCalledWithSopInstanceUid_ExpectItToBeReturned()
        {
            var store = new RemoteAppExecutionRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = _databaseFixture.RemoteAppExecutions.First();
            var actual = await store.GetAsync(expected.SopInstanceUid).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(actual);
            Assert.Equal(expected.SopInstanceUid, actual.SopInstanceUid);
            Assert.Equal(expected.StudyInstanceUid, actual.StudyInstanceUid);
            Assert.Equal(expected.SeriesInstanceUid, actual.SeriesInstanceUid);
            Assert.Equal(expected.WorkflowInstanceId, actual.WorkflowInstanceId);
            Assert.Equal(expected.ExportTaskId, actual.ExportTaskId);
            Assert.Equal(expected.RequestTime, actual.RequestTime);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.CorrelationId, actual.CorrelationId);
            Assert.Equal(expected.OriginalValues, actual.OriginalValues);
        }

        [Fact]
        public async Task GivenARemoteAppExecution_WhenGetAsyncIsCalledWithStudyAndSeriesUids_ExpectItToBeReturned()
        {
            var store = new RemoteAppExecutionRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = _databaseFixture.RemoteAppExecutions.First();
            var actual = await store.GetAsync(expected.WorkflowInstanceId, expected.ExportTaskId, expected.StudyInstanceUid, expected.SeriesInstanceUid).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(actual);
            Assert.Equal(expected.SopInstanceUid, actual.SopInstanceUid);
            Assert.Equal(expected.StudyInstanceUid, actual.StudyInstanceUid);
            Assert.Equal(expected.SeriesInstanceUid, actual.SeriesInstanceUid);
            Assert.Equal(expected.WorkflowInstanceId, actual.WorkflowInstanceId);
            Assert.Equal(expected.ExportTaskId, actual.ExportTaskId);
            Assert.Equal(expected.RequestTime, actual.RequestTime);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.CorrelationId, actual.CorrelationId);
            Assert.Equal(expected.OriginalValues, actual.OriginalValues);
        }

        [Fact]
        public async Task GivenARemoteAppExecution_WhenGetAsyncIsCalledWithRandomSeries_ExpectItToBeReturned()
        {
            var store = new RemoteAppExecutionRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var expected = _databaseFixture.RemoteAppExecutions.First();
            var actual = await store.GetAsync(expected.WorkflowInstanceId, expected.ExportTaskId, expected.StudyInstanceUid, DicomUIDGenerator.GenerateDerivedFromUUID().UID).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(actual);
            Assert.Equal(expected.SopInstanceUid, actual.SopInstanceUid);
            Assert.Equal(expected.StudyInstanceUid, actual.StudyInstanceUid);
            Assert.Equal(expected.SeriesInstanceUid, actual.SeriesInstanceUid);
            Assert.Equal(expected.WorkflowInstanceId, actual.WorkflowInstanceId);
            Assert.Equal(expected.ExportTaskId, actual.ExportTaskId);
            Assert.Equal(expected.RequestTime, actual.RequestTime);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.CorrelationId, actual.CorrelationId);
            Assert.Equal(expected.OriginalValues, actual.OriginalValues);
        }
    }
}
