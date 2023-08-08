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

using System;
using System.IO;
using System.Runtime;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    public class StorageObjectMetadata
    {
        /// <summary>
        /// Gets or sets the temporary path before file is assembled into a payload.
        /// </summary>
        [JsonPropertyName("temporaryPath")]
        public string TemporaryPath { get; set; } = default!;

        /// <summary>
        /// Gets or sets the path the file is stored within the payload directory.
        /// </summary>
        [JsonPropertyName("uploadPath")]
        public string UploadPath { get; set; } = default!;

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        [JsonPropertyName("fileExtension")]
        public string FileExtension { get; init; } = default!;

        /// <summary>
        /// Gets or sets the content type of the file.
        /// </summary>
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = default!;

        /// <summary>
        /// Gets or sets the data stream.
        /// </summary>
        [JsonIgnore]
        public Stream Data { get; set; } = default!;

        [JsonPropertyName("payloadBucketName"), JsonInclude]
        public string PayloadBucketName { get; private set; } = default!;

        [JsonPropertyName("dateMoved"), JsonInclude]
        public DateTime DateMoved { get; private set; } = default!;

        /// <summary>
        /// Gets or set the date time file was uploaded.
        /// </summary>
        [JsonPropertyName("dateUploaded")]
        public DateTime? DateUploaded { get; set; } = default!;

        /// <summary>
        /// Gets the temporary bucket used for storing the file.
        /// </summary>
        [JsonPropertyName("temporaryBucketName"), JsonInclude]
        public string TemporaryBucketName { get; private set; } = default!;

        /// <summary>
        /// Gets or sets whether the file is uploaded to the temporary bucket.
        /// </summary>
        /// <value></value>
        [JsonPropertyName("isUploaded"), JsonInclude]
        public bool IsUploaded { get; private set; } = default!;

        /// <summary>
        /// Gets or sets whether upload failed.
        /// </summary>
        [JsonPropertyName("isUploadFailed"), JsonInclude]
        public bool IsUploadFailed { get; private set; } = default!;

        [JsonPropertyName("isMoveCompleted"), JsonInclude]
        public bool IsMoveCompleted { get; private set; } = default!;

        public StorageObjectMetadata(string fileExtension)
        {
            Guard.Against.NullOrWhiteSpace(fileExtension, nameof(fileExtension));

            if (fileExtension[0] != '.')
            {
                fileExtension = $".{fileExtension}";
            }

            FileExtension = fileExtension;
            IsUploadFailed = false;
        }

        public string GetTempStoragPath(string rootPath)
        {
            Guard.Against.NullOrWhiteSpace(rootPath, nameof(rootPath));
            return $"{rootPath}{FileStorageMetadata.PathSeparator}{TemporaryPath}";
        }

        public string GetPayloadPath(Guid payloadId)
        {
            Guard.Against.Null(payloadId, nameof(payloadId));

            return $"{payloadId}{FileStorageMetadata.PathSeparator}{UploadPath}";
        }

        public void SetUploaded(string bucketName)
        {
            Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));

            TemporaryBucketName = bucketName;
            DateUploaded = DateTime.UtcNow;
            IsUploaded = true;

            if (Data is not null && Data.CanSeek)
            {
                if (Data is FileStream fileStream)
                {
                    var filename = fileStream.Name;
                    Data.Close();
                    Data.Dispose();
                    Data = default!;
                    System.IO.File.Delete(filename);
                }
                else // MemoryStream
                {
                    Data.Close();
                    Data.Dispose();
                    Data = default!;

                    // When IG stores all received/downloaded data in-memory using MemoryStream, LOH grows tremendously and thus impacts the performance and
                    //  memory usage. The following makes sure LOH is compacted after the data is uploaded.
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
#pragma warning disable S1215 // "GC.Collect" should not be called
                    GC.Collect();
#pragma warning restore S1215 // "GC.Collect" should not be called
                }
            }
        }

        public void SetFailed()
        {
            IsUploadFailed = true;
        }

        public void SetMoved(string bucketName)
        {
            Guard.Against.NullOrEmpty(bucketName, nameof(bucketName));

            PayloadBucketName = bucketName;
            DateMoved = DateTime.UtcNow;
            IsMoveCompleted = true;
        }
    }
}
