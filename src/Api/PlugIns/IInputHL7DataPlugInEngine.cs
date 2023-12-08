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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HL7.Dotnetcore;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Api.PlugIns
{
    /// <summary>
    /// <c>IInputDataPlugInEngine</c> processes incoming data receivied from various supported services through
    /// a list of plug-ins based on <see cref="IInputDataPlugIn"/>.
    /// Rules:
    /// <list type="bullet">
    /// <item>SCP: A list of plug-ins can be configured with each AET, and each plug-in is executed in the order stored, enabling piping of the incoming data before each file is uploaded to the storage service.</item>
    /// <item>Incoming data is processed one file at a time and SHALL not wait for the entire study to arrive.</item>
    /// <item>Plug-ins MUST be lightweight and not hinder the upload process.</item>
    /// <item>Plug-ins SHALL not accumulate files in memory or storage for bulk processing.</item>
    /// </list>
    /// </summary>
    public interface IInputHL7DataPlugInEngine
    {
        void Configure(IReadOnlyList<string> pluginAssemblies);

        Task<Tuple<Message, FileStorageMetadata>> ExecutePlugInsAsync(Message hl7File, FileStorageMetadata fileMetadata, Hl7ApplicationConfigEntity configItem);
    }
}
