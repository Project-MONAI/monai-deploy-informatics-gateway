// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using Monai.Deploy.MessageBroker.Common;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    public sealed record DicomFileStorageInfo : FileStorageInfo
    {
        public static readonly string DicomSubDirectoryName = "dcm";
        public static readonly string FilExtension = ".dcm";
        public static readonly string DicomJsonFileExtension = ".json";
        public static readonly string DicomContentType = "application/dicom";
        public static readonly string DicomJsonContentType = System.Net.Mime.MediaTypeNames.Application.Json;

        /// <summary>
        /// The calling AE title of the DICOM instance.
        /// For ACR, this is the Transaction ID of the original request.
        /// Note: this value is same as <seealso cref="Source"></c>
        /// </summary>
        public string CallingAeTitle { get => Source; }

        /// <summary>
        /// The MONAI AE Title that received the DICOM instance.
        /// For ACR request, this field is empty.
        /// </summary>
        public string CalledAeTitle { get; set; }

        /// <summary>
        /// Gets or set the Study Instance UID of the DICOM instance.
        /// </summary>
        public string StudyInstanceUid { get; set; }

        /// <summary>
        /// Gets or set the Series Instance UID of the DICOM instance.
        /// </summary>
        public string SeriesInstanceUid { get; set; }

        /// <summary>
        /// Gets or set the SOP Instance UID of the DICOM instance.
        /// </summary>
        public string SopInstanceUid { get; set; }

        /// <summary>
        /// Gets the relative JSON metadata file path when uploading to storage service.
        /// </summary>
        public string JsonUploadFilePath { get => $"{UploadFilePath}{DicomJsonFileExtension}"; }

        /// <summary>
        /// Gets or sets the full path to the JSON metadata file.
        /// </summary>
        public string JsonFilePath { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<string> FilePaths
        {
            get
            {
                yield return FilePath;
                yield return JsonFilePath;
            }
        }

        protected override string SubDirectoryPath => DicomSubDirectoryName;

        /// <inheritdoc/>
        public override string UploadFilePath => $"{SubDirectoryPath}/{StudyInstanceUid}/{SeriesInstanceUid}/{SopInstanceUid}{FileExtension}";

        public DicomFileStorageInfo()
            : base(FilExtension)
        {
            ContentType = DicomContentType;
        }

        public override BlockStorageInfo ToBlockStorageInfo(string bucket)
        {
            var blockStorage = base.ToBlockStorageInfo(bucket);
            blockStorage.Metadata = JsonUploadFilePath;
            return blockStorage;
        }
    }
}
