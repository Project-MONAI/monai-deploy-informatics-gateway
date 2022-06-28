// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.DicomWeb
{
    internal class StowService : IStowService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly ILogger<StowService> _logger;

        public StowService(IServiceScopeFactory serviceScopeFactory, IOptions<InformaticsGatewayConfiguration> configuration)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var scope = _serviceScopeFactory.CreateScope();
            _logger = scope.ServiceProvider.GetService<ILogger<StowService>>() ?? throw new ServiceNotFoundException(nameof(ILogger<StowService>));
        }

        public async Task<StowResult> StoreAsync(HttpRequest request, string studyInstanceUid, string workflowName, string correlationId, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));

            if (!string.IsNullOrWhiteSpace(studyInstanceUid))
            {
                DicomValidation.ValidateUI(studyInstanceUid);
            }

            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaTypeHeaderValue))
            {
                throw new UnsupportedContentTypeException($"The content type of '{request.ContentType}' is not supported.");
            }

            // TODO: may want to check workflowName in the future.

            var reader = GetRequestReader(mediaTypeHeaderValue);
            var streams = await reader.GetStreams(request, mediaTypeHeaderValue, cancellationToken).ConfigureAwait(false);

            var scope = _serviceScopeFactory.CreateScope();
            var streamsWriter = scope.ServiceProvider.GetService<IStreamsWriter>() ?? throw new ServiceNotFoundException(nameof(IStreamsWriter));

            _logger.SavingStream(streams.Count);
            return await streamsWriter.Save(streams, studyInstanceUid, workflowName, correlationId, request.HttpContext.Connection.RemoteIpAddress.ToString(), cancellationToken).ConfigureAwait(false);
        }

        private IStowRequestReader GetRequestReader(MediaTypeHeaderValue mediaTypeHeaderValue)
        {
            Guard.Against.Null(mediaTypeHeaderValue, nameof(mediaTypeHeaderValue));

            var scope = _serviceScopeFactory.CreateScope();
            var fileSystem = scope.ServiceProvider.GetService<IFileSystem>() ?? throw new ServiceNotFoundException(nameof(IFileSystem));
            if (mediaTypeHeaderValue.MediaType.Equals(ContentTypes.MultipartRelated, StringComparison.OrdinalIgnoreCase))
            {
                var logger = scope.ServiceProvider.GetService<ILogger<MultipartDicomInstanceReader>>() ?? throw new ServiceNotFoundException(nameof(ILogger<MultipartDicomInstanceReader>));
                return new MultipartDicomInstanceReader(_configuration.Value.DicomWeb, logger, fileSystem);
            }

            if (mediaTypeHeaderValue.MediaType.Equals(ContentTypes.ApplicationDicom, StringComparison.OrdinalIgnoreCase))
            {
                var logger = scope.ServiceProvider.GetService<ILogger<SingleDicomInstanceReader>>() ?? throw new ServiceNotFoundException(nameof(ILogger<SingleDicomInstanceReader>));
                return new SingleDicomInstanceReader(_configuration.Value.DicomWeb, logger, fileSystem);
            }

            throw new UnsupportedContentTypeException($"Media type of '{mediaTypeHeaderValue.MediaType}' is not supported.");
        }
    }
}
