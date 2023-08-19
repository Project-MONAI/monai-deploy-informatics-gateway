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
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging.Events;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Test
{
    public class ExternalAppOutgoingTest
    {
        private readonly Mock<ILogger<ExternalAppOutgoing>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly ServiceCollection _serviceCollection;
        private readonly Mock<IRemoteAppExecutionRepository> _repository;
        private readonly IOptions<PlugInConfiguration> _options;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;

        public ExternalAppOutgoingTest()
        {
            _logger = new Mock<ILogger<ExternalAppOutgoing>>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _repository = new Mock<IRemoteAppExecutionRepository>();
            _serviceScope = new Mock<IServiceScope>();
            _options = Options.Create(new PlugInConfiguration());

            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddScoped(p => _logger.Object);
            _serviceCollection.AddScoped(p => _repository.Object);

            _serviceProvider = _serviceCollection.BuildServiceProvider();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void GivenExternalAppOutgoing_TestConstructors()
        {
            Assert.Throws<ArgumentNullException>(() => new ExternalAppOutgoing(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ExternalAppOutgoing(_logger.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new ExternalAppOutgoing(_logger.Object, _serviceScopeFactory.Object, null));
            Assert.Throws<ArgumentNullException>(() => new ExternalAppOutgoing(_logger.Object, _serviceScopeFactory.Object, _options));

            _options.Value.RemoteAppConfigurations.Add(SR.ConfigKey_ReplaceTags, "tag1, tag2");
            var app = new ExternalAppOutgoing(_logger.Object, _serviceScopeFactory.Object, _options);

            Assert.Equal(app.Name, app.GetType().GetCustomAttribute<PlugInNameAttribute>()!.Name);
        }

        [Fact]
        public async Task GivenEmptyReplaceTags_WhenExecuteIsCalledWithoutExistingRecords_ExpectAsync()
        {
            _options.Value.RemoteAppConfigurations.Add(SR.ConfigKey_ReplaceTags, string.Empty);
            var app = new ExternalAppOutgoing(_logger.Object, _serviceScopeFactory.Object, _options);

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var seriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var exportRequest = GenerateExportRequest();
            var message = new ExportRequestDataMessage(exportRequest, "file.dcm");
            var dicom = InstanceGenerator.GenerateDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            _ = await app.ExecuteAsync(dicom, message).ConfigureAwait(false);

            _repository.Verify(p => p.GetAsync(
                It.Is<string>(p => p == exportRequest.WorkflowInstanceId),
                It.Is<string>(p => p == exportRequest.ExportTaskId),
                It.Is<string>(p => p == studyInstanceUid),
                It.Is<string>(p => p == seriesInstanceUid),
                It.IsAny<CancellationToken>()), Times.Once());

            _repository.Verify(p => p.AddAsync(
                It.Is<RemoteAppExecution>(p => AssertRecord(p, dicom, exportRequest, studyInstanceUid, seriesInstanceUid, sopInstanceUid)),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GivenReplaceTags_WhenExecuteIsCalledWithoutExistingRecords_ExpectAsync()
        {
            _options.Value.RemoteAppConfigurations.Add(SR.ConfigKey_ReplaceTags, "StudyInstanceUID,AccessionNumber,PatientID,PatientName");

            var app = new ExternalAppOutgoing(_logger.Object, _serviceScopeFactory.Object, _options);

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var seriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var accessionNumber = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);
            var patientId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);
            var patientName = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);
            var exportRequest = GenerateExportRequest();
            var message = new ExportRequestDataMessage(exportRequest, "file.dcm");
            var dicom = InstanceGenerator.GenerateDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            dicom.Dataset.AddOrUpdate(DicomTag.AccessionNumber, accessionNumber);
            dicom.Dataset.AddOrUpdate(DicomTag.PatientID, patientId);
            dicom.Dataset.AddOrUpdate(DicomTag.PatientName, patientName);

            _ = await app.ExecuteAsync(dicom, message).ConfigureAwait(false);

            _repository.Verify(p => p.GetAsync(
                It.Is<string>(p => p == exportRequest.WorkflowInstanceId),
                It.Is<string>(p => p == exportRequest.ExportTaskId),
                It.Is<string>(p => p == studyInstanceUid),
                It.Is<string>(p => p == seriesInstanceUid),
                It.IsAny<CancellationToken>()), Times.Once());

            _repository.Verify(p => p.AddAsync(
                It.Is<RemoteAppExecution>(p => AssertRecordWithAdditionalTags(p, dicom, exportRequest, studyInstanceUid, seriesInstanceUid, sopInstanceUid, accessionNumber, patientId, patientName)),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GivenExistingRecordWithSameStudy_WhenExecuteIsCalled_ExpectAsync()
        {
            _options.Value.RemoteAppConfigurations.Add(SR.ConfigKey_ReplaceTags, string.Empty);
            var app = new ExternalAppOutgoing(_logger.Object, _serviceScopeFactory.Object, _options);

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var seriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var exportRequest = GenerateExportRequest();
            var message = new ExportRequestDataMessage(exportRequest, "file.dcm");
            var dicom = InstanceGenerator.GenerateDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            _repository.Setup(p => p.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RemoteAppExecution
                {
                    WorkflowInstanceId = exportRequest.WorkflowInstanceId,
                    ExportTaskId = exportRequest.ExportTaskId,
                    StudyInstanceUid = studyInstanceUid,
                    SeriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID
                });

            _ = await app.ExecuteAsync(dicom, message).ConfigureAwait(false);

            _repository.Verify(p => p.GetAsync(
                It.Is<string>(p => p == exportRequest.WorkflowInstanceId),
                It.Is<string>(p => p == exportRequest.ExportTaskId),
                It.Is<string>(p => p == studyInstanceUid),
                It.Is<string>(p => p == seriesInstanceUid),
                It.IsAny<CancellationToken>()), Times.Once());

            _repository.Verify(p => p.AddAsync(
                It.Is<RemoteAppExecution>(p => AssertRecord(p, dicom, exportRequest, studyInstanceUid, seriesInstanceUid, sopInstanceUid)),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GivenExistingRecordWithSameSeries_WhenExecuteIsCalled_ExpectAsync()
        {
            _options.Value.RemoteAppConfigurations.Add(SR.ConfigKey_ReplaceTags, string.Empty);
            var app = new ExternalAppOutgoing(_logger.Object, _serviceScopeFactory.Object, _options);

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var seriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var exportRequest = GenerateExportRequest();
            var message = new ExportRequestDataMessage(exportRequest, "file.dcm");
            var dicom = InstanceGenerator.GenerateDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            _repository.Setup(p => p.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RemoteAppExecution
                {
                    WorkflowInstanceId = exportRequest.WorkflowInstanceId,
                    ExportTaskId = exportRequest.ExportTaskId,
                    StudyInstanceUid = studyInstanceUid,
                    SeriesInstanceUid = seriesInstanceUid
                });

            _ = await app.ExecuteAsync(dicom, message).ConfigureAwait(false);

            _repository.Verify(p => p.GetAsync(
                It.Is<string>(p => p == exportRequest.WorkflowInstanceId),
                It.Is<string>(p => p == exportRequest.ExportTaskId),
                It.Is<string>(p => p == studyInstanceUid),
                It.Is<string>(p => p == seriesInstanceUid),
                It.IsAny<CancellationToken>()), Times.Once());

            _repository.Verify(p => p.AddAsync(
                It.Is<RemoteAppExecution>(p => AssertRecord(p, dicom, exportRequest, studyInstanceUid, seriesInstanceUid, sopInstanceUid)),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        private bool AssertRecord(
            RemoteAppExecution record,
            DicomFile dicom,
            ExportRequestEvent exportRequest,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            return record.WorkflowInstanceId == exportRequest.WorkflowInstanceId &&
                record.ExportTaskId == exportRequest.ExportTaskId &&
                record.OriginalValues[DicomTag.StudyInstanceUID.ToString()] == studyInstanceUid &&
                record.OriginalValues[DicomTag.SeriesInstanceUID.ToString()] == seriesInstanceUid &&
                record.OriginalValues[DicomTag.SOPInstanceUID.ToString()] == sopInstanceUid &&

                record.StudyInstanceUid == dicom.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID) &&
                record.SeriesInstanceUid == dicom.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID) &&
                record.SopInstanceUid == dicom.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
        }

        private bool AssertRecordWithAdditionalTags(
            RemoteAppExecution record,
            DicomFile dicom,
            ExportRequestEvent exportRequest,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string accessionNumber,
            string patientId,
            string patientName)
        {
            return record.WorkflowInstanceId == exportRequest.WorkflowInstanceId &&
                record.ExportTaskId == exportRequest.ExportTaskId &&
                record.OriginalValues[DicomTag.StudyInstanceUID.ToString()] == studyInstanceUid &&
                record.OriginalValues[DicomTag.SeriesInstanceUID.ToString()] == seriesInstanceUid &&
                record.OriginalValues[DicomTag.SOPInstanceUID.ToString()] == sopInstanceUid &&
                record.OriginalValues[DicomTag.AccessionNumber.ToString()] == accessionNumber &&
                record.OriginalValues[DicomTag.PatientID.ToString()] == patientId &&
                record.OriginalValues[DicomTag.PatientName.ToString()] == patientName &&

                record.StudyInstanceUid == dicom.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID) &&
                record.SeriesInstanceUid == dicom.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID) &&
                record.SopInstanceUid == dicom.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
        }

        private ExportRequestEvent GenerateExportRequest() =>
            new()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                ExportTaskId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            };
    }
}
