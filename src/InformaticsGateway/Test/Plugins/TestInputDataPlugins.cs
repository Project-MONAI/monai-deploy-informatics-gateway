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
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Test.Plugins
{
    [PluginName("TestInputDataPluginAddWorkflow")]
    public class TestInputDataPluginAddWorkflow : IInputDataPlugin
    {
        public static readonly string TestString = "TestInputDataPlugin executed!";

        public string Name => GetType().GetCustomAttribute<PluginNameAttribute>()?.Name ?? GetType().Name;

        public Task<(DicomFile dicomFile, FileStorageMetadata fileMetadata)> Execute(DicomFile dicomFile, FileStorageMetadata fileMetadata)
        {
            fileMetadata.Workflows.Add(TestString);
            return Task.FromResult((dicomFile, fileMetadata));
        }
    }

    [PluginName("TestInputDataPluginResumeWorkflow")]
    public class TestInputDataPluginResumeWorkflow : IInputDataPlugin
    {
        public static readonly string WorkflowInstanceId = "ee04a4ac-abb3-412b-b3a7-662c96380379";
        public static readonly string TaskId = "45b20f97-2b38-4b9a-baeb-d15f9d496851";

        public string Name => GetType().GetCustomAttribute<PluginNameAttribute>()?.Name ?? GetType().Name;

        public Task<(DicomFile dicomFile, FileStorageMetadata fileMetadata)> Execute(DicomFile dicomFile, FileStorageMetadata fileMetadata)
        {
            fileMetadata.WorkflowInstanceId = WorkflowInstanceId;
            fileMetadata.TaskId = TaskId;
            return Task.FromResult((dicomFile, fileMetadata));
        }
    }

    [PluginName("TestInputDataPluginModifyDicomFile")]
    public class TestInputDataPluginModifyDicomFile : IInputDataPlugin
    {
        public static readonly DicomTag ExpectedTag = DicomTag.PatientAddress;
        public static readonly string ExpectedValue = "Added by TestInputDataPluginModifyDicomFile";

        public string Name => GetType().GetCustomAttribute<PluginNameAttribute>()?.Name ?? GetType().Name;

        public Task<(DicomFile dicomFile, FileStorageMetadata fileMetadata)> Execute(DicomFile dicomFile, FileStorageMetadata fileMetadata)
        {
            dicomFile.Dataset.Add(ExpectedTag, ExpectedValue);
            return Task.FromResult((dicomFile, fileMetadata));
        }
    }
}
