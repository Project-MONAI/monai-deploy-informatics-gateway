// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
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
    }
}
