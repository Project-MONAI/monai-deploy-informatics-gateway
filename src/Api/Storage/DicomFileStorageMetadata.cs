/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    public sealed record DicomFileStorageMetadata : FileStorageMetadata
    {
        public static readonly string DicomSubDirectoryName = "dcm";
        public static readonly string FileExtension = ".dcm";
        public static readonly string DicomJsonFileExtension = ".json";
        public static readonly string DicomContentType = "application/dicom";
        public static readonly string DicomJsonContentType = System.Net.Mime.MediaTypeNames.Application.Json;

        /// <summary>
        /// The calling AE title of the DICOM instance.
        /// For ACR, this is the Transaction ID of the original request.
        /// Note: this value is same as <see cref="FileStorageMetadata.Source"/>
        /// </summary>
        [JsonIgnore]
        public string CallingAeTitle { get => Source; }

        /// <summary>
        /// The MONAI AE Title that received the DICOM instance.
        /// For ACR request, this field is empty.
        /// </summary>
        [JsonPropertyName("calledAeTitle")]
        public string CalledAeTitle { get; set; } = default!;

        /// <summary>
        /// Gets or set the Study Instance UID of the DICOM instance.
        /// </summary>
        [JsonPropertyName("studyInstanceUid")]
        public string StudyInstanceUid { get; init; } = default!;

        /// <summary>
        /// Gets or set the Series Instance UID of the DICOM instance.
        /// </summary>
        [JsonPropertyName("seriesInstanceUid")]
        public string SeriesInstanceUid { get; init; } = default!;

        /// <summary>
        /// Gets or set the SOP Instance UID of the DICOM instance.
        /// </summary>
        [JsonPropertyName("sopInstanceUid")]
        public string SopInstanceUid { get; init; } = default!;

        /// <inheritdoc/>
        [JsonIgnore]
        public override string DataTypeDirectoryName { get => DicomSubDirectoryName; }

        /// <inheritdoc/>
        [JsonPropertyName("file")]
        public override StorageObjectMetadata File { get; set; } = default!;

        /// <summary>
        /// Gets the storage object metadata for the JSON file associated with the DICOM instance.
        /// </summary>
        [JsonPropertyName("jsonFile")]
        public StorageObjectMetadata JsonFile { get; set; } = default!;

        [JsonIgnore]
        public override bool IsUploaded => base.IsUploaded && JsonFile.IsUploaded;

        [JsonIgnore]
        public override bool IsMoveCompleted => base.IsMoveCompleted && JsonFile.IsMoveCompleted;

        [JsonIgnore]
        public override bool IsUploadFailed => base.IsUploadFailed && JsonFile.IsUploadFailed;
        /// <summary>
        /// DO NOT USE
        /// This constructor is intended for JSON serializer.
        /// Due to limitation in current version of .NET, the constructor must be public.
        /// https://github.com/dotnet/runtime/issues/31511
        /// </summary>
        [JsonConstructor]
        public DicomFileStorageMetadata() { }

        public DicomFileStorageMetadata(string associationId, string identifier, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
            : base(associationId.ToString(), identifier)
        {
            Guard.Against.NullOrWhiteSpace(associationId, nameof(associationId));
            Guard.Against.NullOrWhiteSpace(identifier, nameof(identifier));
            Guard.Against.NullOrWhiteSpace(identifier, nameof(identifier));
            Guard.Against.NullOrWhiteSpace(identifier, nameof(identifier));
            Guard.Against.NullOrWhiteSpace(identifier, nameof(identifier));

            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            File = new StorageObjectMetadata(FileExtension)
            {
                TemporaryPath = string.Join(PathSeparator, associationId, DataTypeDirectoryName, $"{Guid.NewGuid()}{FileExtension}"),
                UploadPath = string.Join(PathSeparator, DataTypeDirectoryName, StudyInstanceUid, SeriesInstanceUid, $"{SopInstanceUid}{FileExtension}"),
                ContentType = DicomContentType,
            };

            JsonFile = new StorageObjectMetadata(DicomJsonFileExtension)
            {
                TemporaryPath = $"{File.TemporaryPath}{DicomJsonFileExtension}",
                UploadPath = $"{File.UploadPath}{DicomJsonFileExtension}",
                ContentType = DicomJsonContentType,
            };
        }

        public override void SetFailed()
        {
            base.SetFailed();
            JsonFile.SetFailed();
        }
    }
}
