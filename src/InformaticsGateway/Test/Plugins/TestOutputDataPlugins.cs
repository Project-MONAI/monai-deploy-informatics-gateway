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

using System.Reflection;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Test.Plugins
{

    [PluginName("TestOutputDataPluginAddMessage")]
    public class TestOutputDataPluginAddMessage : IOutputDataPlugin
    {
        public static readonly string ExpectedValue = "Hello from TestOutputDataPluginAddMessage";

        public string Name => GetType().GetCustomAttribute<PluginNameAttribute>()?.Name ?? GetType().Name;

        public Task<(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)> Execute(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)
        {
            exportRequestDataMessage.Messages.Add(ExpectedValue);
            return Task.FromResult((dicomFile, exportRequestDataMessage));
        }

    }
    [PluginName("TestOutputDataPluginModifyDicomFile")]
    public class TestOutputDataPluginModifyDicomFile : IOutputDataPlugin
    {
        public static readonly DicomTag ExpectedTag = DicomTag.PatientAddress;
        public static readonly string ExpectedValue = "Added by TestOutputDataPluginModifyDicomFile";

        public string Name => GetType().GetCustomAttribute<PluginNameAttribute>()?.Name ?? GetType().Name;

        public Task<(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)> Execute(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)
        {
            dicomFile.Dataset.Add(ExpectedTag, ExpectedValue);
            return Task.FromResult((dicomFile, exportRequestDataMessage));
        }

    }
}
