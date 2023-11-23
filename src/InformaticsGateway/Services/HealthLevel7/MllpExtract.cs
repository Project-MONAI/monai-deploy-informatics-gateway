using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using HL7.Dotnetcore;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal sealed class MllpExtract : IMllpExtract
    {
        private readonly ILogger<MllpExtract> _logger;
        private readonly IHl7ApplicationConfigRepository _hl7ApplicationConfigRepository;
        private readonly IExternalAppDetailsRepository _externalAppDetailsRepository;

        public MllpExtract(IHl7ApplicationConfigRepository hl7ApplicationConfigRepository, IExternalAppDetailsRepository externalAppDetailsRepository, ILogger<MllpExtract> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hl7ApplicationConfigRepository = hl7ApplicationConfigRepository ?? throw new ArgumentNullException(nameof(hl7ApplicationConfigRepository));
            _externalAppDetailsRepository = externalAppDetailsRepository ?? throw new ArgumentNullException(nameof(externalAppDetailsRepository));
        }


        public async Task<Message> ExtractInfo(Hl7FileStorageMetadata meta, Message message)
        {
            try
            {
                // load the config
                var config = await _hl7ApplicationConfigRepository.GetAllAsync().ConfigureAwait(false);
                if (config == null)
                {
                    _logger.Hl7NoConfig();
                    return message;
                }
                _logger.Hl7ConfigLoaded($"Config: {config}");
                // get config for vendorId
                var configItem = GetConfig(config, message);
                if (configItem == null)
                {
                    _logger.Hl7NoMatchingConfig(message.HL7Message);
                    return message;
                }
                // extract data for the given fields
                // Use Id to get record from Db
                var details = await GetExtAppDetails(configItem, message);

                if (details is null)
                {
                    _logger.Hl7ExtAppDetailsNotFound();
                    return message;
                }

                // fill in meta data with workflowInstance and Task ID
                // repopulate message with data from record

                meta.WorkflowInstanceId = details.WorkflowInstanceId;
                meta.TaskId = details.ExportTaskID;
                meta.ChangeCorrelationId(_logger, details.CorrelationId);

                if (string.IsNullOrEmpty(details.DestinationFolder) is false)
                {
                    meta.File.DestinationFolderOverride = true;
                    meta.File.UploadPath = $"{details.DestinationFolder}/{meta.Id}{Hl7FileStorageMetadata.FileExtension}";
                }
                message = RepopulateMessage(configItem, details, message);
            }
            catch (Exception ex)
            {
                _logger.Hl7ExceptionThrow(ex);
            }
            return message;
        }

        private async Task<ExternalAppDetails?> GetExtAppDetails(Hl7ApplicationConfigEntity hl7ApplicationConfigEntity, Message message)
        {
            var tagId = message.GetValue(hl7ApplicationConfigEntity.DataLink.Key);
            var type = hl7ApplicationConfigEntity.DataLink.Value;
            switch (type)
            {
                case DataLinkType.PatientId:
                    return await _externalAppDetailsRepository.GetByPatientIdOutboundAsync(tagId, new CancellationToken());
                case DataLinkType.StudyInstanceUid:
                    return await _externalAppDetailsRepository.GetByStudyIdOutboundAsync(tagId, new CancellationToken());
                default:
                    break;
            }

            throw new Exception($"Invalid DataLinkType: {type}");
        }

        internal static Hl7ApplicationConfigEntity? GetConfig(List<Hl7ApplicationConfigEntity> config, Message message)
        {
            foreach (var item in config)
            {
                var t = message.GetValue(item.SendingId.Key);
                if (item.SendingId.Value == message.GetValue(item.SendingId.Key))
                {
                    return item;
                }
            }
            return null;
        }

        private Message RepopulateMessage(Hl7ApplicationConfigEntity config, ExternalAppDetails details, Message message)
        {
            foreach (var item in config.DataMapping)
            {
                var tag = DicomTag.Parse(item.Value);
                // these are the only two fields we have at the point
                if (tag == DicomTag.PatientID)
                {
                    var oldvalue = message.GetValue(item.Key);
                    message.SetValue(item.Key, details.PatientId);
                    _logger.ChangingHl7Values(item.Key, oldvalue, details.PatientId);
                    if (message.HL7Message.Contains(oldvalue))
                    {
                        var newMess = message.HL7Message.Replace(oldvalue, details.PatientId);
                        message = new Message(newMess);
                        message.ParseMessage();
                    }
                }
                else if (tag == DicomTag.StudyInstanceUID)
                {
                    var oldvalue = message.GetValue(item.Key);
                    message.SetValue(item.Key, details.StudyInstanceUid);
                    _logger.ChangingHl7Values(item.Key, oldvalue, details.StudyInstanceUid);
                    if (message.HL7Message.Contains(oldvalue))
                    {
                        var newMess = message.HL7Message.Replace(oldvalue, details.StudyInstanceUid);
                        message = new Message(newMess);
                        message.ParseMessage();
                    }
                }
            }
            return message;
        }
    }
}
