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


using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using HL7.Dotnetcore;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Api.Mllp
{
    public sealed class MllpExtract : IMllpExtract
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


        public async Task<Message> ExtractInfo(Hl7FileStorageMetadata meta, Message message, Hl7ApplicationConfigEntity configItem)
        {
            try
            {
                // extract data for the given fields
                // Use Id to get record from Db
                var details = await GetExtAppDetails(configItem, message).ConfigureAwait(false);

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
                    meta.File.UploadPath = $"{details.DestinationFolder}/{meta.DataTypeDirectoryName}/{meta.Id}{Hl7FileStorageMetadata.FileExtension}";
                }
                message = RepopulateMessage(configItem, details, message);
            }
            catch (Exception ex)
            {
                _logger.Hl7ExceptionThrow(ex);
            }
            return message;
        }

        public async Task<Hl7ApplicationConfigEntity?> GetConfigItem(Message message)
        {
            // load the config
            var config = await _hl7ApplicationConfigRepository.GetAllAsync().ConfigureAwait(false);
            if (config == null)
            {
                _logger.Hl7NoConfig();
                return null;
            }
            _logger.Hl7ConfigLoaded($"Config: {JsonSerializer.Serialize(config)}");
            // get config for vendorId
            var configItem = GetConfig(config, message);
            if (configItem == null)
            {
                _logger.Hl7NoMatchingConfig(message.HL7Message);
                return null;
            }
            return configItem;
        }

        private async Task<ExternalAppDetails?> GetExtAppDetails(Hl7ApplicationConfigEntity hl7ApplicationConfigEntity, Message message)
        {
            var tagId = message.GetValue(hl7ApplicationConfigEntity.DataLink.Key);
            var type = hl7ApplicationConfigEntity.DataLink.Value;
            switch (type)
            {
                case DataLinkType.PatientId:
                    return await _externalAppDetailsRepository.GetByPatientIdOutboundAsync(tagId, new CancellationToken()).ConfigureAwait(false);
                case DataLinkType.StudyInstanceUid:
                    return await _externalAppDetailsRepository.GetByStudyIdOutboundAsync(tagId, new CancellationToken()).ConfigureAwait(false);
                default:
                    break;
            }

            throw new Exception($"Invalid DataLinkType: {type}");
        }

        internal Hl7ApplicationConfigEntity? GetConfig(List<Hl7ApplicationConfigEntity> config, Message message)
        {
            foreach (var item in config)
            {
                var sendingId = message.GetValue(item.SendingId.Key);
                if (item.SendingId.Value == sendingId)
                {
                    _logger.Hl7FoundMatchingConfig(sendingId, JsonSerializer.Serialize(item));
                    return item;
                }
                else
                {
                    _logger.Hl7NotMatchingConfig(sendingId, item.SendingId.Value);
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
                        message.ParseMessage(true);
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
                        message.ParseMessage(true);
                    }
                }
            }
            return message;
        }
    }
}
