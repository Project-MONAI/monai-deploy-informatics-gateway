// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using Ardalis.GuardClauses;
using Monai.Deploy.MessageBroker.Common;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    public abstract record FileStorageInfo
    {
        protected abstract string SubDirectoryPath { get; }

        /// <summary>
        /// Gets the relative file path when uploading to storage service.
        /// </summary>
        public abstract string UploadFilePath { get; }

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        public string FileExtension { get; init; }

        /// <summary>
        /// Gets the unique (user-defined) ID of the file.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets the correlation ID of the file.
        /// For SCP received DICOM instances: use internally generated unique association ID.
        /// For ACR retrieved DICOM/FHIR files: use the original transaction ID embedded in the request.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the source of the file.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the full path to the file.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets a list of workflows designated for the file.
        /// </summary>
        public string[] Workflows { get; private set; }

        /// <summary>
        /// Gets or sets the DateTime that the file was received.
        /// </summary>
        /// <value></value>
        public DateTime DateReceived { get; init; }

        /// <summary>
        /// Gets or sets the DateTime that the file was uploaded to storage service.
        /// </summary>
        /// <value></value>
        public DateTime DateUploaded { get; private set; }

        /// <summary>
        /// Gets or sets whether the file is uploaded.
        /// </summary>
        /// <value></value>
        public bool IsUploaded { get; private set; }

        /// <summary>
        /// Gets or sets the content type of the file.
        /// </summary>
        public string ContentType { get; protected set; }

        /// <summary>
        /// All file paths in the temporary storage associated with the instance.
        /// </summary>
        public virtual IEnumerable<string> FilePaths
        {
            get
            {
                yield return FilePath;
            }
        }

        protected FileStorageInfo(string fileExtension)
        {
            Guard.Against.NullOrWhiteSpace(fileExtension, nameof(fileExtension));

            if (fileExtension[0] != '.')
            {
                fileExtension = $".{fileExtension}";
            }

            FileExtension = fileExtension;
            DateReceived = DateTime.UtcNow;
            DateUploaded = DateTime.MinValue;
            IsUploaded = false;
        }

        /// <summary>
        /// Workflows to be launched on MONAI Workflow Manager, ignoring data routing agent.
        /// </summary>
        /// <param name="workflows">List of workflows.</param>
        public void SetWorkflows(params string[] workflows)
        {
            Guard.Against.NullOrEmpty(workflows, nameof(workflows));

            Workflows = workflows.Clone() as string[];
        }

        public virtual BlockStorageInfo ToBlockStorageInfo(string bucket)
        {
            return new BlockStorageInfo
            {
                Path = UploadFilePath,
                Metadata = string.Empty,
            };
        }

        public void SetUploaded()
        {
            DateUploaded = DateTime.UtcNow;
            IsUploaded = true;
        }
    }
}
