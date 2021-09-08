using Dicom;
using Dicom.Network;
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
        public static ILogger Logger { get; set; }

        public DicomScpFixture()
        {
        }

        public void Start(int port = 11104)
        {
            if (_server is null)
            {
                _server = DicomServer.Create<CStoreSCP>(
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
        }
    }

    public class CStoreSCP : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
    {
        private static readonly DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
        {
               DicomTransferSyntax.ExplicitVRLittleEndian,
               DicomTransferSyntax.ExplicitVRBigEndian,
               DicomTransferSyntax.ImplicitVRLittleEndian
        };

        public CStoreSCP(INetworkStream stream, Encoding fallbackEncoding, Dicom.Log.Logger log)
            : base(stream, fallbackEncoding, log)
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

        public DicomCStoreResponse OnCStoreRequest(DicomCStoreRequest request)
        {
            DicomScpFixture.Logger.LogInformation($"Instance received {request.SOPInstanceUID.UID}");
            return new DicomCStoreResponse(request, DicomScpFixture.DicomStatus);
        }

        public void OnCStoreRequestException(string tempFileName, Exception e)
        {
        }

        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }
    }
}
