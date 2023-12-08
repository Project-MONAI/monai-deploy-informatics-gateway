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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    /// <summary>
    /// A new instance of <c>ScpServiceInternal</c> is created for every new association.
    /// </summary>
    internal abstract class ScpServiceInternalBase :
        DicomService,
        IDicomServiceProvider,
        IDicomCEchoProvider,
        IDicomCStoreProvider
    {
        private readonly DicomAssociationInfo _associationInfo;
        private readonly ILogger _logger;
        protected IApplicationEntityManager? AssociationDataProvider;
        private IDisposable? _loggerScope;
        protected Guid AssociationId;
        private DateTimeOffset? _associationReceived;

        public ScpServiceInternalBase(INetworkStream stream, Encoding fallbackEncoding, ILogger logger, DicomServiceDependencies dicomServiceDependencies)
                : base(stream, fallbackEncoding, logger, dicomServiceDependencies)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _associationInfo = new DicomAssociationInfo();
        }

        public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            _logger?.CEchoReceived();
            return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
        }

        public void OnConnectionClosed(Exception exception)
        {
            if (exception != null)
            {
                _logger?.ConnectionClosedWithException(exception);
            }

            _loggerScope?.Dispose();
            Interlocked.Decrement(ref ScpServiceBase.ActiveConnections);

            try
            {
                var repo = AssociationDataProvider!.GetService<IDicomAssociationInfoRepository>();
                _associationInfo.Disconnect();
                repo?.AddAsync(_associationInfo).Wait();
                _logger?.ConnectionClosed(_associationInfo.CorrelationId, _associationInfo.CallingAeTitle, _associationInfo.CalledAeTitle, _associationInfo.Duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger?.ErrorSavingDicomAssociationInfo(AssociationId, ex);
            }
        }

        public abstract Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request);

        public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
        {
            _logger?.CStoreFailed(e);
            _associationInfo.Errors = $"Failed to store file: {e}";
            return Task.CompletedTask;
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            _logger?.CStoreAbort(source, reason);
            _associationInfo.Errors = $"{source} - {reason}";
        }

        /// <summary>
        /// Start timer only if a receive association release request is received.
        /// </summary>
        /// <returns></returns>
        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            var associationElapsed = TimeSpan.Zero;
            if (_associationReceived.HasValue)
            {
                associationElapsed = DateTimeOffset.UtcNow.Subtract(_associationReceived.Value);
            }

            _logger?.CStoreAssociationReleaseRequest(associationElapsed);
            return SendAssociationReleaseResponseAsync();
        }

        public async Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            Interlocked.Increment(ref ScpServiceBase.ActiveConnections);
            _associationReceived = DateTimeOffset.UtcNow;
            AssociationDataProvider = (UserState as IApplicationEntityManager)!;

            if (AssociationDataProvider is null)
            {
                _associationInfo.Errors = $"Internal error: association data provider not found.";
                throw new ServiceException($"{nameof(UserState)} must be an instance of IAssociationDataProvider");
            }

            AssociationId = Guid.NewGuid();
            var associationIdStr = $"#{AssociationId} {association.RemoteHost}:{association.RemotePort}";

            _loggerScope = _logger!.BeginScope(new LoggingDataDictionary<string, object> { { "Association", associationIdStr } });
            _logger.CStoreAssociationReceived(association.RemoteHost, association.RemotePort);

            _associationInfo.CallingAeTitle = association.CallingAE;
            _associationInfo.CalledAeTitle = association.CalledAE;
            _associationInfo.RemoteHost = association.RemoteHost;
            _associationInfo.RemotePort = association.RemotePort;
            _associationInfo.CorrelationId = AssociationId.ToString();

            if (!await IsValidSourceAeAsync(association.CallingAE, association.RemoteHost).ConfigureAwait(false))
            {
                _associationInfo.Errors = $"Invalid source. Called AE: {association.CalledAE}. Calling AE: {association.CallingAE}. IP: {association.RemoteHost}.";

                await SendAssociationRejectAsync(
                    DicomRejectResult.Permanent,
                    DicomRejectSource.ServiceUser,
                    DicomRejectReason.CallingAENotRecognized).ConfigureAwait(false);
            }

            if (!await IsValidCalledAeAsync(association.CalledAE).ConfigureAwait(false))
            {
                _associationInfo.Errors = "Invalid MONAI AE Title. Called AE: {association.CalledAE}. Calling AE: {association.CallingAE}. IP: {association.RemoteHost}.";

                await SendAssociationRejectAsync(
                    DicomRejectResult.Permanent,
                    DicomRejectSource.ServiceUser,
                    DicomRejectReason.CalledAENotRecognized).ConfigureAwait(false);
            }

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification)
                {
                    if (!AssociationDataProvider.Configuration.Value.Dicom.Scp.EnableVerification)
                    {
                        _associationInfo.Errors = "Verification service disabled. Called AE: {association.CalledAE}. Calling AE: {association.CallingAE}. IP: {association.RemoteHost}.";
                        _logger?.VerificationServiceDisabled();
                        await SendAssociationRejectAsync(
                            DicomRejectResult.Permanent,
                            DicomRejectSource.ServiceUser,
                            DicomRejectReason.ApplicationContextNotSupported
                        ).ConfigureAwait(false);
                    }
                    pc.AcceptTransferSyntaxes(AssociationDataProvider.Configuration.Value.Dicom.Scp.VerificationServiceTransferSyntaxes.ToDicomTransferSyntaxArray());
                }
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                {
                    if (!AssociationDataProvider.CanStore)
                    {
                        _associationInfo.Errors = "Disk pressure. Called AE: {association.CalledAE}. Calling AE: {association.CallingAE}. IP: {association.RemoteHost}.";
                        await SendAssociationRejectAsync(
                            DicomRejectResult.Permanent,
                            DicomRejectSource.ServiceUser,
                            DicomRejectReason.NoReasonGiven).ConfigureAwait(false);
                    }
                    // Accept any proposed TS
                    pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
                }
            }

            await SendAssociationAcceptAsync(association).ConfigureAwait(false);
        }

        private async Task<bool> IsValidCalledAeAsync(string calledAe)
        {
            return await AssociationDataProvider!.IsAeTitleConfiguredAsync(calledAe).ConfigureAwait(false);
        }

        private async Task<bool> IsValidSourceAeAsync(string callingAe, string host)
        {
            if (!AssociationDataProvider!.Configuration.Value.Dicom.Scp.RejectUnknownSources) return true;

            return await AssociationDataProvider.IsValidSourceAsync(callingAe, host).ConfigureAwait(false);
        }
    }
}
