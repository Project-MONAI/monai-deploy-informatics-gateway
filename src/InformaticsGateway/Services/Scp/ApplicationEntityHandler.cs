// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ApplicationEntityHandler
    {
        private readonly MonaiApplicationEntity _configuration;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IDicomToolkit _dicomToolkit;
        private readonly ILogger<ApplicationEntityHandler> _logger;
        private readonly DicomJsonOptions _dicomJsonOptions;

        public ApplicationEntityHandler(
            MonaiApplicationEntity monaiApplicationEntity,
            IPayloadAssembler payloadAssembler,
            IDicomToolkit dicomToolkit,
            ILogger<ApplicationEntityHandler> logger,
            DicomJsonOptions dicomJsonOptions)
        {
            _configuration = monaiApplicationEntity ?? throw new ArgumentNullException(nameof(monaiApplicationEntity));
            _payloadAssembler = payloadAssembler ?? throw new ArgumentNullException(nameof(payloadAssembler));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dicomJsonOptions = dicomJsonOptions;
        }

        internal async Task HandleInstance(DicomCStoreRequest request, DicomFileStorageInfo info)
        {
            if (_configuration.IgnoredSopClasses.Contains(request.SOPClassUID.UID))
            {
                _logger.Log(LogLevel.Information, $"Instance ignored due to matching SOP Class UID {request.SOPClassUID.UID}");
                return;
            }

            if (_configuration.Workflows.Any())
            {
                info.SetWorkflows(_configuration.Workflows.ToArray());
            }

            await SaveDicomInstance(request, info.FilePath, info.DicomJsonFilePath);

            var dicomTag = FellowOakDicom.DicomTag.Parse(_configuration.Grouping);
            if (request.Dataset.TryGetSingleValue<string>(dicomTag, out string key))
            {
                await _payloadAssembler.Queue(key, info, _configuration.Timeout);
            }
        }

        private async Task SaveDicomInstance(DicomCStoreRequest request, string filename, string metadataFilename)
        {
            _logger.Log(LogLevel.Debug, $"Preparing to save {filename}");
            await _dicomToolkit.Save(request.File, filename, metadataFilename, _dicomJsonOptions);
            _logger.Log(LogLevel.Information, $"Instance saved {filename}");
        }
    }
}
