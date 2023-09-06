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
using System.Threading.Tasks;
using FellowOakDicom.Network;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal interface IApplicationEntityHandler
    {
        void Configure(MonaiApplicationEntity monaiApplicationEntity, DicomJsonOptions dicomJsonOptions, bool validateDicomValuesOnJsonSerialization);

        Task HandleInstanceAsync(DicomCStoreRequest request, string calledAeTitle, string callingAeTitle, Guid associationId, StudySeriesSopAids aids);
    }
}
