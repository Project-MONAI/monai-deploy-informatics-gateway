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

using System.Threading.Tasks;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api.Models;

namespace Monai.Deploy.InformaticsGateway.Api.PlugIns
{
    /// <summary>
    /// <c>IOutputDataPlugIn</c> enables lightweight data processing over incoming data received from supported data ingestion
    /// services.
    /// Refer to <see cref="IOutputDataPlugInEngine" /> for additional details.
    /// </summary>
    public interface IOutputDataPlugIn
    {
        string Name { get; }

        Task<(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)> ExecuteAsync(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage);
    }
}
