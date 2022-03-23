// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
