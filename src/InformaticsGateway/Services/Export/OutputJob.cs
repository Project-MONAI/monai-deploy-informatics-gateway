// Copyright 2020 - 2021 MONAI Consortium
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

using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Common;
using System.Linq;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    internal class OutputJob : TaskResponse
    {
        public byte[] FileContent { get; private set; }

        public OutputJob(TaskResponse task, byte[] fileContent)
        {
            Guard.Against.Null(task, nameof(task));
            Guard.Against.Null(fileContent, nameof(fileContent));
            FileContent = fileContent;

            CopyBaseProperties(task);
        }

        private void CopyBaseProperties(TaskResponse task)
        {
            var properties = task.GetType().GetProperties();

            properties.ToList().ForEach(property =>
            {
                var isPresent = GetType().GetProperty(property.Name);
                if (isPresent != null)
                {
                    //If present get the value and map it
                    var value = task.GetType().GetProperty(property.Name).GetValue(task, null);
                    GetType().GetProperty(property.Name).SetValue(this, value, null);
                }
            });
        }
    }
}