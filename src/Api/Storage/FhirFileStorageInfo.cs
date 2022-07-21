/*
 * Copyright 2021-2022 MONAI Consortium
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

using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a FHIR resource and storage hierarchy/path.
    /// </summary>
    public sealed record FhirFileStorageInfo : FileStorageInfo
    {
        public static readonly string FhirSubDirectoryName = "ehr";
        public static readonly string JsonFilExtension = ".json";
        public static readonly string XmlFilExtension = ".xml";

        /// <summary>
        /// The transaction ID of the original ACR request.
        /// Note: this value is same as <seealso cref="Source"></c>
        /// </summary>
        public string TransactionId { get => Source; }

        /// <summary>
        /// Gets or set the FHIR resource type.
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Gets or set the FHIR resource ID.
        /// </summary>
        public string ResourceId { get; set; }

        protected override string SubDirectoryPath => FhirSubDirectoryName;

        public override string UploadFilePath => $"{SubDirectoryPath}/{ResourceType}-{ResourceId}{FileExtension}";

        public FhirFileStorageInfo(FhirStorageFormat fhirFileFormat)
            : base(fhirFileFormat == FhirStorageFormat.Json ? JsonFilExtension : XmlFilExtension)
        {
            ContentType = fhirFileFormat == FhirStorageFormat.Json ? System.Net.Mime.MediaTypeNames.Application.Json : System.Net.Mime.MediaTypeNames.Application.Xml;
        }
    }
}
