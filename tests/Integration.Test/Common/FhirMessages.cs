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

using static Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions.FhirDefinitions;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class FhirMessages
    {
        public string MediaType { get; set; }
        public FileFormat FileFormat { get; set; }
        public Dictionary<string, string> Files { get; set; } = new Dictionary<string, string>();
    }
}
