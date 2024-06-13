/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ScpServiceInternal : ScpServiceInternalBase
    {


        private readonly DicomAssociationInfo _associationInfo;
        private readonly ILogger _logger;
        //private IApplicationEntityManager? _associationDataProvider;
        //private IDisposable? _loggerScope;
        //private Guid _associationId;
        //private DateTimeOffset? _associationReceived;

        public ScpServiceInternal(INetworkStream stream, Encoding fallbackEncoding, ILogger logger, DicomServiceDependencies dicomServiceDependencies)
                : base(stream, fallbackEncoding, logger, dicomServiceDependencies)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _associationInfo = new DicomAssociationInfo();
        }
        public override async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            try
            {
                _logger?.TransferSyntaxUsed(request.TransferSyntax);
                var payloadId = await AssociationDataProvider!.HandleCStoreRequest(request, Association.CalledAE, Association.CallingAE, AssociationId, Common.ScpInputTypeEnum.WorkflowTrigger).ConfigureAwait(false);
                _associationInfo.FileReceived(payloadId);
                return new DicomCStoreResponse(request, DicomStatus.Success);
            }
            catch (InsufficientStorageAvailableException ex)
            {
                _logger?.CStoreFailedDueToLowStorageSpace(ex);
                _associationInfo.Errors = $"Failed to store file due to low disk space: {ex}";
                return new DicomCStoreResponse(request, DicomStatus.ResourceLimitation);
            }
            catch (System.IO.IOException ex) when ((ex.HResult & 0xFFFF) == Constants.ERROR_HANDLE_DISK_FULL || (ex.HResult & 0xFFFF) == Constants.ERROR_DISK_FULL)
            {
                _logger?.CStoreFailedWithNoSpace(ex);
                _associationInfo.Errors = $"Failed to store file due to low disk space: {ex}";
                return new DicomCStoreResponse(request, DicomStatus.StorageStorageOutOfResources);
            }
            catch (Exception ex)
            {
                _logger?.CStoreFailed(ex);
                _associationInfo.Errors = $"Failed to store file: {ex}";
                return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
            }
        }

    }
}
