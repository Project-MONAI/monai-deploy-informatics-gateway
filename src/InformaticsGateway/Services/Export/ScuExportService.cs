/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.Messaging.Common;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    internal class ScuExportService : ExportServiceBase
    {
        private readonly ILogger<ScuExportService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;

        protected override ushort Concurrency { get; }
        public override string RoutingKey { get; }
        public override string ServiceName => "DICOM Export Service";

        public ScuExportService(
            ILogger<ScuExportService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IDicomToolkit dicomToolkit)
            : base(logger, configuration, serviceScopeFactory, dicomToolkit)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            RoutingKey = $"{configuration.Value.Messaging.Topics.ExportRequestPrefix}.{_configuration.Value.Dicom.Scu.AgentName}";
            Concurrency = _configuration.Value.Dicom.Scu.MaximumNumberOfAssociations;
        }

        protected override async Task ProcessMessage(MessageReceivedEventArgs eventArgs)
        {
            await BaseProcessMessage(eventArgs);
        }

        protected override async Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new Api.LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequestData.ExportTaskId }, { "CorrelationId", exportRequestData.CorrelationId }, { "Filename", exportRequestData.Filename } });

            foreach (var destinationName in exportRequestData.Destinations)
            {
                await HandleDesination(exportRequestData, destinationName, cancellationToken).ConfigureAwait(false);
            }

            return exportRequestData;
        }
    }
}
