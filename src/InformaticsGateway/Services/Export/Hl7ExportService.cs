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
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Api.Mllp;
using Monai.Deploy.Messaging.Common;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    internal class Hl7ExportService : ExportServiceBase
    {
        private readonly ILogger<Hl7ExportService> _logger;
        private readonly InformaticsGatewayConfiguration _configuration;
        private readonly IMllpService _mllpService;

        protected override ushort Concurrency { get; }
        public override string RoutingKey { get; }
        public override string ServiceName => "DICOM Export HL7 Service";


        public Hl7ExportService(
            ILogger<Hl7ExportService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IDicomToolkit dicomToolkit)
            : base(logger, configuration, serviceScopeFactory, dicomToolkit)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));

            _mllpService = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMllpService>();
            RoutingKey = $"{configuration.Value.Messaging.Topics.ExportHL7}";
            ExportCompleteTopic = $"{configuration.Value.Messaging.Topics.ExportHl7Complete}";
            Concurrency = _configuration.Dicom.Scu.MaximumNumberOfAssociations;
        }


        protected override Task ProcessMessage(MessageReceivedEventArgs eventArgs)
        {
            return BaseProcessMessage(eventArgs);
        }


        protected override async Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new Api.LoggingDataDictionary<string, object>
            {
                { "ExportTaskId", exportRequestData.ExportTaskId },
                { "CorrelationId", exportRequestData.CorrelationId },
                { "Filename", exportRequestData.Filename }
            });

            foreach (var destinationName in exportRequestData.Destinations)
            {
                await HandleDesination(exportRequestData, destinationName, cancellationToken).ConfigureAwait(false);
            }

            return exportRequestData;
        }

        protected override async Task HandleDesination(ExportRequestDataMessage exportRequestData, string destinationName, CancellationToken cancellationToken)
        {
            Guard.Against.Null(exportRequestData, nameof(exportRequestData));

            var destination = await GetHL7Destination(exportRequestData, destinationName, cancellationToken).ConfigureAwait(false);
            if (destination is null)
            {
                return;
            }

            try
            {
                await ExecuteHl7Export(exportRequestData, destination!, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HandleCStoreException(ex, exportRequestData);
            }
        }

        private async Task ExecuteHl7Export(
            ExportRequestDataMessage exportRequestData,
            HL7DestinationEntity destination,
            CancellationToken cancellationToken) => await Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(
                   _configuration.Export.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.HL7ExportErrorWithRetry(timeSpan, retryCount, exception);
                   })
               .ExecuteAsync(async () =>
               {
                   await _mllpService.SendMllp(
                       Dns.GetHostAddresses(destination.HostIp)[0],
                           destination.Port, Encoding.UTF8.GetString(exportRequestData.FileContent),
                           cancellationToken
                       ).ConfigureAwait(false);
               }).ConfigureAwait(false);


        private async Task<HL7DestinationEntity> LookupDestinationAsync(string destinationName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(destinationName))
            {
                throw new ConfigurationException("Export task does not have destination set.");
            }

            using var scope = ServiceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IHL7DestinationEntityRepository>();
            var destination = await repository.FindByNameAsync(destinationName, cancellationToken).ConfigureAwait(false);

            return destination is null
                ? throw new ConfigurationException($"Specified destination '{destinationName}' does not exist.")
                : destination;
        }

        private async Task<HL7DestinationEntity?> GetHL7Destination(ExportRequestDataMessage exportRequestData, string destinationName, CancellationToken cancellationToken)
        {
            try
            {
                return await LookupDestinationAsync(destinationName, cancellationToken).ConfigureAwait(false);
            }
            catch (ConfigurationException ex)
            {
                HandleCStoreException(ex, exportRequestData);
                return null;
            }
        }

        protected override Task<ExportRequestDataMessage> ExecuteOutputDataEngineCallback(ExportRequestDataMessage exportDataRequest)
        {
            return Task.FromResult(exportDataRequest);
        }

    }
}
