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

using System.Reflection;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging.Events;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Test
{
    public class DicomReidentifierTest
    {
        private readonly Mock<ILogger<DicomReidentifier>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly ServiceCollection _serviceCollection;
        private readonly Mock<IRemoteAppExecutionRepository> _repository;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;

        public DicomReidentifierTest()
        {
            _logger = new Mock<ILogger<DicomReidentifier>>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _repository = new Mock<IRemoteAppExecutionRepository>();
            _serviceScope = new Mock<IServiceScope>();

            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddScoped(p => _logger.Object);
            _serviceCollection.AddScoped(p => _repository.Object);

            _serviceProvider = _serviceCollection.BuildServiceProvider();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void GivenDicomDeidentifier_TestConstructors()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>(() => new DicomReidentifier(null, null));

            Assert.Throws<ArgumentNullException>(() => new DicomReidentifier(_logger.Object, null));

            var app = new DicomReidentifier(_logger.Object, _serviceScopeFactory.Object);

            Assert.Equal(app.Name, app.GetType().GetCustomAttribute<PlugInNameAttribute>()!.Name);
        }

        [Fact]
        public async Task GivenIncomingInstance_WhenExecuteIsCalledWithMissingRecord_ExpectErrorToBeLogged()
        {
            var app = new DicomReidentifier(_logger.Object, _serviceScopeFactory.Object);

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var seriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var dicom = InstanceGenerator.GenerateDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            var metadata = new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), studyInstanceUid, seriesInstanceUid, sopInstanceUid, DataService.DIMSE, "calling", "called");

            _repository.Setup(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(RemoteAppExecution));

            _ = await app.ExecuteAsync(dicom, metadata).ConfigureAwait(false);

            _repository.Verify(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLogging($"Cannot find entry for incoming instance {sopInstanceUid}.", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task GivenIncomingInstance_WhenExecuteIsCalledWithRecord_ExpectDataToBeFilled()
        {
            var app = new DicomReidentifier(_logger.Object, _serviceScopeFactory.Object);

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var seriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var dicom = InstanceGenerator.GenerateDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            var metadata = new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), studyInstanceUid, seriesInstanceUid, sopInstanceUid, DataService.DIMSE, "calling", "called");
            var record = new RemoteAppExecution
            {
                CorrelationId = Guid.NewGuid().ToString(),
                ExportTaskId = Guid.NewGuid().ToString(),
                Id = Guid.NewGuid(),
                RequestTime = DateTimeOffset.UtcNow,
            };

            record.OriginalValues.Add(DicomTag.StudyInstanceUID.ToString(), DicomUIDGenerator.GenerateDerivedFromUUID().UID);
            record.OriginalValues.Add(DicomTag.SeriesInstanceUID.ToString(), DicomUIDGenerator.GenerateDerivedFromUUID().UID);
            record.OriginalValues.Add(DicomTag.SOPInstanceUID.ToString(), DicomUIDGenerator.GenerateDerivedFromUUID().UID);
            record.OriginalValues.Add(DicomTag.PatientID.ToString(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16));
            record.OriginalValues.Add(DicomTag.AccessionNumber.ToString(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16));
            record.OriginalValues.Add(DicomTag.StudyDescription.ToString(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16));

            _repository.Setup(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(record);

            _ = await app.ExecuteAsync(dicom, metadata).ConfigureAwait(false);

            _repository.Verify(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLogging($"Cannot find entry for incoming instance {sopInstanceUid}.", LogLevel.Error, Times.Never());

            Assert.Equal(record.OriginalValues[DicomTag.StudyInstanceUID.ToString()], dicom.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
            Assert.Equal(record.OriginalValues[DicomTag.SeriesInstanceUID.ToString()], dicom.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty));
            Assert.Equal(record.OriginalValues[DicomTag.PatientID.ToString()], dicom.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
            Assert.Equal(record.OriginalValues[DicomTag.AccessionNumber.ToString()], dicom.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));
            Assert.Equal(record.OriginalValues[DicomTag.StudyDescription.ToString()], dicom.Dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, string.Empty));

            Assert.Equal(record.CorrelationId, metadata.CorrelationId);
            Assert.Equal(record.ExportTaskId, metadata.TaskId);
            Assert.Equal(record.WorkflowInstanceId, metadata.WorkflowInstanceId);
        }
    }
}
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
