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
using HL7.Dotnetcore;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Test.PlugIns
{
    [PlugInName("TestInputHL7DataPlugInAddWorkflow")]
    public class TestInputHL7DataPlugInAddWorkflow : IInputHL7DataPlugIn
    {
        public static readonly string TestString = "HOSPITAL changed!";

        public string Name => GetType().GetCustomAttribute<PlugInNameAttribute>()?.Name ?? GetType().Name;

        public Task<(Message hl7Message, FileStorageMetadata fileMetadata)> ExecuteAsync(Message hl7File, FileStorageMetadata fileMetadata)
        {
            hl7File.SetValue("MSH.3", TestString);
            hl7File = new Message(hl7File.SerializeMessage(false));
            hl7File.ParseMessage();
            fileMetadata.Workflows.Add(TestString);
            return Task.FromResult((hl7File, fileMetadata));
        }
    }
}
