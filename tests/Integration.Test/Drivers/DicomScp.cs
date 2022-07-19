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

using System.Text;
using FellowOakDicom;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Drivers
{
    public class ServerData
    {
        public FeatureContext Context { get; set; }
        public Dictionary<string, string> Instances { get; set; } = new Dictionary<string, string>();
        public ISpecFlowOutputHelper OutputHelper { get; set; }
    }

    internal class CStoreScp : DicomService, IDicomServiceProvider, IDicomCStoreProvider
    {
        private static readonly Object SyncLock = new Object();
        internal static readonly string PayloadsRoot = "./payloads";

        public CStoreScp(INetworkStream stream, Encoding fallbackEncoding, ILogger log, ILogManager logManager, INetworkManager network, ITranscoderManager transcoder)
            : base(stream, fallbackEncoding, log, logManager, network, transcoder)
        {
        }

        public void OnConnectionClosed(Exception exception)
        {
            if (exception is not null)
            {
                Console.WriteLine("Connection closed with error {0}.", exception);
            }
            else
            {
                Console.WriteLine("Connection closed.");
            }
        }

        public Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            if (UserState is not ServerData data)
            {
                throw new Exception("UserState is not instance of ServerData.");
            }

            try
            {
                var key = request.File.GenerateFileName();
                lock (SyncLock)
                {
                    data.Instances.Add(key, request.File.CalculateHash());
                }
                data.OutputHelper.WriteLine("Instance received {0}", key);

                return Task.FromResult(new DicomCStoreResponse(request, DicomStatus.Success));
            }
            catch (Exception ex)
            {
                data.OutputHelper.WriteLine("Exception 'OnCStoreRequestAsync': {0}", ex);
                return Task.FromResult(new DicomCStoreResponse(request, DicomStatus.ProcessingFailure));
            }
        }

        public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
        {
            Console.WriteLine($"Exception 'OnCStoreRequestExceptionAsync': {e}");
            return Task.CompletedTask;
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            Console.WriteLine($"Exception 'OnReceiveAbort': source {source}, reason {reason}");
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            return SendAssociationReleaseResponseAsync();
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                {
                    pc.AcceptTransferSyntaxes(pc.GetTransferSyntaxes().ToArray());
                }
            }

            return SendAssociationAcceptAsync(association);
        }
    }
}
