// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Monai.Deploy.InformaticsGateway.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monai.Deploy.InformaticsGateway.Api
{
    /// <summary>
    /// MONAI Application Entity
    /// MONAI's SCP AE Title is used to accept incoming associations and can, optionally, map to multiple applications.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     "name": "brain-tumor",
    ///     "aeTitle": "BrainTumorModel"
    /// }
    /// </code>
    /// <code>
    /// {
    ///     "name": "COVID-19",
    ///     "aeTitle": "COVID-19",
    ///     "applications": [ "EXAM", "Delta", "DeltaPlus"]
    /// }
    /// </code>
    /// </example>
    public class MonaiApplicationEntity
    {
        /// <summary>
        /// Gets or sets the name of a MONAI DICOM application entity.
        /// This value must be unique.
        /// </summary>
        [Key, Column(Order = 0)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the AE TItle.
        /// </summary>
        public string AeTitle { get; set; }

        /// <summary>
        /// Optional field to the AE to one or more applications.
        /// </summary>
        public List<string> Applications { get; set; }

        public MonaiApplicationEntity()
        {
            SetDefaultValues();
        }

        public void SetDefaultValues()
        {
            if (string.IsNullOrWhiteSpace(Name))
                Name = AeTitle;

            if (Applications.IsNull())
                Applications = new List<string>();
        }
    }
}