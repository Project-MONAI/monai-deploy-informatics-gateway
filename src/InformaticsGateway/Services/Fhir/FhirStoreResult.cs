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

using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Fhir
{
    internal class FhirStoreResult
    {
        /// <summary>
        /// HTTP result status code.
        /// </summary>
        public int StatusCode { get; internal set; }

        /// <summary>
        /// User posted data.
        /// </summary>
        public string RawData { get; internal set; } = string.Empty;

        /// <summary>
        ///
        /// </summary>
        public FhirFileStorageMetadata? Metadata { get; internal set; }

        /// <summary>
        /// ResourceType found in the original FHIR resource.
        /// </summary>
        public string InternalResourceType { get; internal set; } = string.Empty;

        /// <summary>
        /// ResourceType specified by the user in the POST URI.
        /// </summary>
        public string ResourceType { get; internal set; } = string.Empty;
    }
}
