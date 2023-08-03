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

using System.Collections.Generic;
using System.Threading.Tasks;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Api
{
    /// <summary>
    /// <c>IInputDataPluginEngine</c> processes incoming data receivied from various supported services through
    /// a list of plug-ins based on <see cref="IInputDataPlugin"/>.
    /// Rules:
    /// <list type="bullet">
    /// <item>A list of plug-ins can be included with each export request, and each plug-in is executed in the order stored, processing one file at a time, enabling piping of the data before each file is exported.</item>
    /// <item>Plugins MUST be lightweight and not hinder the export process</item>
    /// <item>Plugins SHALL not accumulate files in memory or storage for bulk processing</item>
    /// </list>
    /// </summary>
    public interface IInputDataPluginEngine
    {
        void Configure(IReadOnlyList<string> pluginAssemblies);

        Task<(DicomFile dicomFile, FileStorageMetadata fileMetadata)> ExecutePlugins(DicomFile dicomFile, FileStorageMetadata fileMetadata);
    }
}
