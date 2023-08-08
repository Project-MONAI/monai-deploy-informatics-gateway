﻿/*
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

using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Test.Plugins
{

    public class TestOutputDataPluginAddMessage : IOutputDataPlugin
    {
        public static readonly string ExpectedValue = "Hello from TestOutputDataPluginAddMessage";

        public Task<(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)> Execute(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)
        {
            exportRequestDataMessage.Messages.Add(ExpectedValue);
            return Task.FromResult((dicomFile, exportRequestDataMessage));
        }

    }
    public class TestOutputDataPluginModifyDicomFile : IOutputDataPlugin
    {
        public static readonly DicomTag ExpectedTag = DicomTag.PatientAddress;
        public static readonly string ExpectedValue = "Added by TestOutputDataPluginModifyDicomFile";

        public Task<(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)> Execute(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)
        {
            dicomFile.Dataset.Add(ExpectedTag, ExpectedValue);
            return Task.FromResult((dicomFile, exportRequestDataMessage));
        }

    }
}
