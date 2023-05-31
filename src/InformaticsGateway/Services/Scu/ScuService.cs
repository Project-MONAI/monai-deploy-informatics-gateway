/*
 * Copyright 2022 MONAI Consortium
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
using Ardalis.GuardClauses;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;

namespace Monai.Deploy.InformaticsGateway.Services.Scu
{
    internal sealed class ScuService : IHostedService, IDisposable, IMonaiService
    {
        private readonly IServiceScope _scope;
        private readonly ILogger<ScuService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IScuQueue _workQueue;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public string ServiceName => "DICOM SCU Service";

        public ScuService(IServiceScopeFactory serviceScopeFactory,
                          ILogger<ScuService> logger,
                          IOptions<InformaticsGatewayConfiguration> configuration)
        {
            Guard.Against.Null(serviceScopeFactory);

            _scope = serviceScopeFactory.CreateScope();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _workQueue = _scope.ServiceProvider.GetService<IScuQueue>() ?? throw new ServiceNotFoundException(nameof(IScuQueue));
        }

        private async Task BackgroundProcessingAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceRunning(ServiceName);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var item = _workQueue.Dequeue(cancellationToken);

                    var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, item.CancellationToken);

                    ProcessThread(item, linkedCancellationToken.Token);
                }
                catch (ObjectDisposedException ex)
                {
                    _logger.ServiceDisposed(ServiceName, ex);
                }
                catch (Exception ex)
                {
                    if (ex is InvalidOperationException || ex is OperationCanceledException)
                    {
                        _logger.ServiceInvalidOrCancelled(ServiceName, ex);
                    }
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.ServiceCancelled(ServiceName);
        }

        private void ProcessThread(ScuWorkRequest request, CancellationToken cancellationToken)
        {
            Task.Run(() => Process(request, cancellationToken));
        }

        private async Task Process(ScuWorkRequest request, CancellationToken cancellationToken)
        {
            ScuWorkResponse response = null;
            try
            {
                response = request.RequestType switch
                {
                    RequestType.CEcho => await HandleCEchoRequest(request, cancellationToken).ConfigureAwait(false),
                    _ => new ScuWorkResponse { Status = ResponseStatus.Failure, Error = ResponseError.UnsupportedRequestType },
                };
            }
            catch (Exception exception)
            {
                response = new ScuWorkResponse { Status = ResponseStatus.Failure, Error = ResponseError.Unhandled, Message = exception.Message };
            }
            finally
            {
                request.Complete(response);
            }
        }

        private async Task<ScuWorkResponse> HandleCEchoRequest(ScuWorkRequest request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var scuResponse = new ScuWorkResponse();
            var manualResetEvent = new ManualResetEventSlim();
            try
            {
                using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> {
                    { "CorrelationId", request.CorrelationId },
                    { "Remote Host/IP", request.HostIp },
                    { "Remote Port", request.Port },
                    { "Remote AE Title", request.AeTitle }
                });

                var client = DicomClientFactory.Create(
                                   request.HostIp,
                                   request.Port,
                                   false,
                                   _configuration.Value.Dicom.Scu.AeTitle,
                                   request.AeTitle);

                client.AssociationAccepted += (sender, args) =>
                {
                    _logger.ScuAssociationAccepted();
                };
                client.AssociationRejected += (sender, args) =>
                {
                    _logger.ScuAssociationRejected();
                };
                client.AssociationReleased += (sender, args) =>
                {
                    _logger.ScuAssociationReleased();
                };
                client.ServiceOptions.LogDataPDUs = _configuration.Value.Dicom.Scu.LogDataPdus;
                client.ServiceOptions.LogDimseDatasets = _configuration.Value.Dicom.Scu.LogDimseDatasets;
                client.NegotiateAsyncOps();

                var cechoRequest = new DicomCEchoRequest();
                cechoRequest.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        scuResponse.Status = ResponseStatus.Success;
                        _logger.CEchoSuccess();
                    }
                    else
                    {
                        scuResponse.Status = ResponseStatus.Failure;
                        scuResponse.Error = ResponseError.CEchoError;
                        scuResponse.Message = response.Status.ToString();
                        _logger.CEchoFailure(scuResponse.Message);
                    }
                };
                await client.AddRequestAsync(cechoRequest).ConfigureAwait(false);
                await client.SendAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DicomAssociationRejectedException ex)
            {
                scuResponse.Status = ResponseStatus.Failure;
                scuResponse.Error = ResponseError.AssociationRejected;
                scuResponse.Message = ex.Message;
                _logger.CEchoFailure(ex.Message);
            }
            catch (DicomAssociationAbortedException ex)
            {
                scuResponse.Status = ResponseStatus.Failure;
                scuResponse.Error = ResponseError.AssociationAborted;
                scuResponse.Message = ex.Message;
                _logger.CEchoFailure(ex.Message);
            }
            catch (Exception ex)
            {
                scuResponse.Status = ResponseStatus.Failure;
                scuResponse.Error = ResponseError.Unhandled;
                scuResponse.Message = ex.Message;
                _logger.CEchoFailure(ex.Message);
            }
            finally
            {
                manualResetEvent.Set();
            }
            return scuResponse;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(async () =>
            {
                await BackgroundProcessingAsync(cancellationToken).ConfigureAwait(false);
            }, CancellationToken.None);

            Status = ServiceStatus.Running;
            _logger.ServiceRunning(ServiceName);
            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _scope.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
