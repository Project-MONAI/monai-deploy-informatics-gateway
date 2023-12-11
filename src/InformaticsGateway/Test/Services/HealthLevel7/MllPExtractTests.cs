
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


using System.Threading;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using System;
using Monai.Deploy.InformaticsGateway.Api;
using System.Collections.Generic;
using HL7.Dotnetcore;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Api.Mllp;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using System.Threading.Tasks;
using Monai.Deploy.InformaticsGateway.Api.Models;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.Test.Services.HealthLevel7
{
    public class MllPExtractTests
    {
        private const string SampleMessage = "MSH|^~\\&|MD|MD HOSPITAL|MD Test|MONAI Deploy|202207130000|SECURITY|MD^A01^ADT_A01|MSG00001|P|2.8|||<ACK>|\r\n";
        private const string ABCDEMessage = "MSH|^~\\&|Rayvolve|ABCDE|RIS|{InstitutionName}|{YYYYMMDDHHMMSS}||ORU^R01|{UniqueIdentifier}|P|2.5\r\nPID|{StudyInstanceUID}|{AccessionNumber}\r\nOBR|{StudyInstanceUID}||{AccessionNumber}|Rayvolve^{AlgorithmUsed}||||||||||||{AccessionNumber}|||||||F||{PriorityValues, ex: A^ASAP^HL70078}\r\nTQ1|||||||||{PriorityValues, ex: A^ASAP^HL70078}\r\nOBX|1|ST|113014^DICOM Study^DCM||{StudyInstanceUID}||||||O\r\nOBX|2|TX|59776-5^Procedure Findings^LN||{Textual findingsm, ex:\"Fracture detected\")}|||{Abnormal flag, ex : A^Abnormal^HL70078}|||F||||{ACR flag, ex : RID49482^Category 3 Non critical Actionable Finding^RadLex}\r\n";
        private const string VendorMessage = "MSH|^~\\&|Vendor INSIGHT CXR |Vendor Inc.|||20231130091315||ORU^R01|ORU20231130091315834|P|2.4||||||UNICODE UTF-8\r\nPID|1||2.25.82866891564990604580806081805518233357\r\nPV1|1|O\r\nORC|RE||||SC\r\nOBR|1|||CXR0001^Chest X-ray Report|||20230418142212.134||||||||||||||||||P|||||||Vendor\r\nNTE|1||Bilateral lungs are clear without remarkable opacity.\\X0A\\Cardiomediastinal contour appears normal.\\X0A\\Pneumothorax is not seen.\\X0A\\Pleural Effusion is present on the bilateral sides.\\X0A\\\\X0A\\Threshold value\\X0A\\Atelectasis: 15\\X0A\\Calcification: 15\\X0A\\Cardiomegaly: 15\\X0A\\Consolidation: 15\\X0A\\Fibrosis: 15\\X0A\\Mediastinal Widening: 15\\X0A\\Nodule: 15\\X0A\\Pleural Effusion: 15\\X0A\\Pneumoperitoneum: 15\\X0A\\Pneumothorax: 15\\X0A\\\r\nZDS|2.25.97606619386020057921123852316852071139||2.25.337759261491022538565548360794987622189|Vendor INSIGHT CXR|v3.1.5.3\r\nOBX|1|NM|RAB0001^Abnormality Score||50.82||||||P|||20230418142212.134||Vendor";

        private readonly Mock<ILogger<MllpExtract>> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly MllpExtract _sut;
        private readonly Mock<IHl7ApplicationConfigRepository> _l7ApplicationConfigRepository = new Mock<IHl7ApplicationConfigRepository>();
        private readonly Mock<IExternalAppDetailsRepository> _externalAppDetailsRepository = new Mock<IExternalAppDetailsRepository>();

        public MllPExtractTests()
        {
            _logger = new Mock<ILogger<MllpExtract>>();
            _cancellationTokenSource = new CancellationTokenSource();
            _sut = new MllpExtract(_l7ApplicationConfigRepository.Object, _externalAppDetailsRepository.Object, _logger.Object);
        }

        [Fact(DisplayName = "Constructor Should Throw on missing arguments")]
        public void Constructor_Should_Throw_on_missing_arguments()
        {
            Assert.Throws<ArgumentNullException>(() => new MllpExtract(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new MllpExtract(_l7ApplicationConfigRepository.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new MllpExtract(_l7ApplicationConfigRepository.Object, _externalAppDetailsRepository.Object, null));

            new MllpExtract(_l7ApplicationConfigRepository.Object, _externalAppDetailsRepository.Object, _logger.Object);
        }

        [Fact(DisplayName = "ParseConfig Should Return Correct Item")]
        public void ParseConfig_Should_Return_Correct_Item()
        {
            var correctid = new Guid("00000000-0000-0000-0000-000000000002");
            var azCorrectid = new Guid("00000000-0000-0000-0000-000000000001");
            var configs = new List<Hl7ApplicationConfigEntity> {
                new Hl7ApplicationConfigEntity{ Id= new Guid("00000000-0000-0000-0000-000000000001"), SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "ABCDE" } },
                new Hl7ApplicationConfigEntity{ Id= correctid, SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "MD HOSPITAL"  } },
            };

            var message = new Message(SampleMessage);
            var isParsed = message.ParseMessage();

            var config = MllpExtract.GetConfig(configs, message);
            Assert.Equal(correctid, config?.Id);

            message = new Message(ABCDEMessage);
            isParsed = message.ParseMessage();

            config = MllpExtract.GetConfig(configs, message);
            Assert.Equal(azCorrectid, config?.Id);
        }

        [Fact(DisplayName = "Should Set MetaData On Hl7FileStorageMetadata Object")]
        public async Task Should_Set_MetaData_On_Hl7FileStorageMetadata_Object()
        {
            var correctid = new Guid("00000000-0000-0000-0000-000000000002");
            var azCorrectid = new Guid("00000000-0000-0000-0000-000000000001");
            var configs = new List<Hl7ApplicationConfigEntity> {
                new Hl7ApplicationConfigEntity{
                    Id= new Guid("00000000-0000-0000-0000-000000000001"),
                    SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "ABCDE" }
                    ,DataLink = new DataKeyValuePair{ Key = "PID.1", Value = DataLinkType.StudyInstanceUid }
                },
                new Hl7ApplicationConfigEntity{ Id= correctid, SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "MD HOSPITAL"  } },
            };

            _l7ApplicationConfigRepository
                .Setup(x => x.GetAllAsync(new CancellationToken()))
                .ReturnsAsync(configs);

            _externalAppDetailsRepository.Setup(x => x.GetByStudyIdOutboundAsync("{StudyInstanceUID}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalAppDetails
                {
                    WorkflowInstanceId = "WorkflowInstanceId2",
                    ExportTaskID = "ExportTaskID2",
                    CorrelationId = "CorrelationId2",
                    DestinationFolder = "DestinationFolder2"
                });

            var message = new Message(ABCDEMessage);
            var isParsed = message.ParseMessage();

            var meatData = new Hl7FileStorageMetadata { Id = "metaId", File = new StorageObjectMetadata("txt") };

            var configItem = await _sut.GetConfigItem(message);
            await _sut.ExtractInfo(meatData, message, configItem);

            Assert.Equal("WorkflowInstanceId2", meatData.WorkflowInstanceId);
            Assert.Equal("ExportTaskID2", meatData.TaskId);
            Assert.Equal("CorrelationId2", meatData.CorrelationId);
            Assert.StartsWith("DestinationFolder2", meatData.File.UploadPath);
        }

        [Fact(DisplayName = "Should Set Original Patient And Study Uid")]
        public async Task Should_Set_Original_Patient_And_Study_Uid()
        {
            var correctid = new Guid("00000000-0000-0000-0000-000000000002");
            var azCorrectid = new Guid("00000000-0000-0000-0000-000000000001");
            var configs = new List<Hl7ApplicationConfigEntity> {
                new Hl7ApplicationConfigEntity{
                    Id= new Guid("00000000-0000-0000-0000-000000000001"),
                    SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "ABCDE" }
                    ,DataLink = new DataKeyValuePair{ Key = "PID.1", Value = DataLinkType.StudyInstanceUid },
                    DataMapping = new List<StringKeyValuePair>{
                        new StringKeyValuePair { Key = "PID.1", Value = DicomTag.StudyInstanceUID.ToString() },
                        new StringKeyValuePair { Key = "OBR.3", Value = DicomTag.PatientID.ToString() },
                    }

                },
                new Hl7ApplicationConfigEntity{ Id= correctid, SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "MD HOSPITAL"  } },
            };

            _l7ApplicationConfigRepository
                .Setup(x => x.GetAllAsync(new CancellationToken()))
                .ReturnsAsync(configs);

            _externalAppDetailsRepository.Setup(x => x.GetByStudyIdOutboundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalAppDetails
                {
                    WorkflowInstanceId = "WorkflowInstanceId2",
                    ExportTaskID = "ExportTaskID2",
                    CorrelationId = "CorrelationId2",
                    DestinationFolder = "DestinationFolder2",
                    PatientId = "PatentID",
                    StudyInstanceUid = "StudyInstanceId"
                });

            var message = new Message(ABCDEMessage);
            var isParsed = message.ParseMessage();

            var te = message.GetValue("OBR.1");

            var meatData = new Hl7FileStorageMetadata { Id = "metaId", File = new StorageObjectMetadata("txt") };

            var configItem = await _sut.GetConfigItem(message);
            message = await _sut.ExtractInfo(meatData, message, configItem);

            Assert.Equal("PatentID", message.GetValue("OBR.3"));
            Assert.Equal("PatentID", message.GetValue("PID.2"));
            Assert.Equal("StudyInstanceId", message.GetValue("PID.1"));
            Assert.Equal("StudyInstanceId", message.GetValue("OBR.1"));

        }

        [Fact(DisplayName = "ParseConfig Should Return Correct Item for vendor")]
        public void ParseConfig_Should_Return_Correct_Item_For_Vendor()
        {
            var correctid = new Guid("00000000-0000-0000-0000-000000000002");
            var azCorrectid = new Guid("00000000-0000-0000-0000-000000000001");

            var configs = new List<Hl7ApplicationConfigEntity> {
                new Hl7ApplicationConfigEntity{ Id= new Guid("00000000-0000-0000-0000-000000000001"), SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "ABCDE" } },
                new Hl7ApplicationConfigEntity{ Id= correctid, SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "Vendor Inc."  } },
            };

            var message = new Message(VendorMessage);
            var isParsed = message.ParseMessage();

            var config = MllpExtract.GetConfig(configs, message);
            Assert.Equal(correctid, config?.Id);

            message = new Message(ABCDEMessage);
            isParsed = message.ParseMessage();

            config = MllpExtract.GetConfig(configs, message);
            Assert.Equal(azCorrectid, config?.Id);
        }
    }
}
