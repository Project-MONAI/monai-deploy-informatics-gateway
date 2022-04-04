// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Ardalis.GuardClauses;

namespace Monai.Deploy.Storage.Common
{
    /// <summary>
    /// Represents a file stored on the virtual storage device.
    /// </summary>
    public class VirtualFileInfo
    {
        /// <summary>
        /// Gets or set the name of the file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the (non-rooted) path of the file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or set the etag of the file
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the size of the file
        /// </summary>
        public ulong Size { get; set; }

        /// <summary>
        /// Gets or set the last modified date time of the file
        /// </summary>
        public DateTime? LastModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the file
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }

        public VirtualFileInfo(string filename, string filePath, string etag, ulong size)
        {
            Guard.Against.Null(filename, nameof(filename));
            Guard.Against.Null(filePath, nameof(filePath));
            Guard.Against.Null(etag, nameof(etag));

            Filename = filename;
            FilePath = filePath;
            ETag = etag;
            Size = size;

            Metadata = new Dictionary<string, string>();
        }

    }
}
