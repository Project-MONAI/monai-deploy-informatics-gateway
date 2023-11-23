
using Monai.Deploy.InformaticsGateway.Services.HealthLevel7;
using System.Threading;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using System;
using Monai.Deploy.InformaticsGateway.Api;
using System.Collections.Generic;
using HL7.Dotnetcore;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using System.Threading.Tasks;
using Monai.Deploy.InformaticsGateway.Api.Models;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.Test.Services.HealthLevel7
{
    public class MllPExtractTests
    {
        private const string SampleMessage = "MSH|^~\\&|MD|MD HOSPITAL|MD Test|MONAI Deploy|202207130000|SECURITY|MD^A01^ADT_A01|MSG00001|P|2.8|||<ACK>|\r\n";
        private const string AzMedMessage = "MSH|^~\\&|Rayvolve|AZMED|RIS|{InstitutionName}|{YYYYMMDDHHMMSS}||ORU^R01|{UniqueIdentifier}|P|2.5\r\nPID|{StudyInstanceUID}|{AccessionNumber}\r\nOBR|{StudyInstanceUID}||{AccessionNumber}|Rayvolve^{AlgorithmUsed}||||||||||||{AccessionNumber}|||||||F||{PriorityValues, ex: A^ASAP^HL70078}\r\nTQ1|||||||||{PriorityValues, ex: A^ASAP^HL70078}\r\nOBX|1|ST|113014^DICOM Study^DCM||{StudyInstanceUID}||||||O\r\nOBX|2|TX|59776-5^Procedure Findings^LN||{Textual findingsm, ex:\"Fracture detected\")}|||{Abnormal flag, ex : A^Abnormal^HL70078}|||F||||{ACR flag, ex : RID49482^Category 3 Non critical Actionable Finding^RadLex}\r\n";

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
                new Hl7ApplicationConfigEntity{ Id= new Guid("00000000-0000-0000-0000-000000000001"), SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "AZMED" } },
                new Hl7ApplicationConfigEntity{ Id= correctid, SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "MD HOSPITAL"  } },
            };

            var message = new Message(SampleMessage);
            var isParsed = message.ParseMessage();

            var config = MllpExtract.GetConfig(configs, message);
            Assert.Equal(correctid, config?.Id);

            message = new Message(AzMedMessage);
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
                    SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "AZMED" }
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

            var message = new Message(AzMedMessage);
            var isParsed = message.ParseMessage();

            var meatData = new Hl7FileStorageMetadata();

            await _sut.ExtractInfo(meatData, message);

            Assert.Equal("WorkflowInstanceId2", meatData.WorkflowInstanceId);
            Assert.Equal("ExportTaskID2", meatData.TaskId);
            Assert.Equal("CorrelationId2", meatData.CorrelationId);
            Assert.Equal("DestinationFolder2", meatData.PayloadId);
        }

        [Fact(DisplayName = "Should Set Original Patient And Study Uid")]
        public async Task Should_Set_Original_Patient_And_Study_Uid()
        {
            var correctid = new Guid("00000000-0000-0000-0000-000000000002");
            var azCorrectid = new Guid("00000000-0000-0000-0000-000000000001");
            var configs = new List<Hl7ApplicationConfigEntity> {
                new Hl7ApplicationConfigEntity{
                    Id= new Guid("00000000-0000-0000-0000-000000000001"),
                    SendingId = new StringKeyValuePair{ Key = "MSH.4", Value = "AZMED" }
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

            var message = new Message(AzMedMessage);
            var isParsed = message.ParseMessage();

            var te = message.GetValue("OBR.1");

            var meatData = new Hl7FileStorageMetadata();

            message = await _sut.ExtractInfo(meatData, message);

            Assert.Equal("PatentID", message.GetValue("OBR.3"));
            Assert.Equal("PatentID", message.GetValue("PID.2"));
            Assert.Equal("StudyInstanceId", message.GetValue("PID.1"));
            Assert.Equal("StudyInstanceId", message.GetValue("OBR.1"));

        }
    }
}
