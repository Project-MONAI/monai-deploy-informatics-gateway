﻿// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using FellowOakDicom;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Test.Shared
{
    public class DicomScpFixture : IDisposable
    {
        internal static string AETITLE = "STORESCP";
        private IDicomServer _server;
        public static DicomStatus DicomStatus { get; set; } = DicomStatus.Success;
        public static Microsoft.Extensions.Logging.ILogger Logger { get; set; }

        public DicomScpFixture()
        {
        }

        public void Start(int port = 11104)
        {
            if (_server is null)
            {
                _server = DicomServerFactory.Create<CStoreSCP>(
                    NetworkManager.IPv4Any,
                    port);

                if (_server.Exception is not null)
                {
                    throw _server.Exception;
                }
            }
        }

        public void Dispose()
        {
            _server?.Dispose();
            _server = null;
            GC.SuppressFinalize(this);
        }
    }

    public class CStoreSCP : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
    {
        public CStoreSCP(INetworkStream stream, Encoding fallbackEncoding, FellowOakDicom.Log.ILogger log, ILogManager logManager, INetworkManager network, ITranscoderManager transcoder)
                : base(stream, fallbackEncoding, log, logManager, network, transcoder)
        {
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            if (association.CalledAE == "ABORT")
            {
                return SendAbortAsync(DicomAbortSource.ServiceUser, DicomAbortReason.NotSpecified);
            }

            if (association.CalledAE != DicomScpFixture.AETITLE)
            {
                return SendAssociationRejectAsync(
                    DicomRejectResult.Permanent,
                    DicomRejectSource.ServiceUser,
                    DicomRejectReason.CalledAENotRecognized);
            }

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification) pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None) pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
            }

            return SendAssociationAcceptAsync(association);
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            return SendAssociationReleaseResponseAsync();
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
        }

        public void OnConnectionClosed(Exception exception)
        {
        }

        public Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            DicomScpFixture.Logger.LogInformation($"Instance received {request.SOPInstanceUID.UID}");
            return Task.FromResult(new DicomCStoreResponse(request, DicomScpFixture.DicomStatus));
        }

        public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
        {
            return Task.CompletedTask;
        }

        public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
        }
    }
}
