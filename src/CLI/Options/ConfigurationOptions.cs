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

using Ardalis.GuardClauses;
using System;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ConfigurationOptions
    {
        public string Endpoint { get; set; }

        public void Validate()
        {
            Guard.Against.NullOrEmpty(Endpoint, nameof(Endpoint));
            if (!Uri.IsWellFormedUriString(Endpoint, UriKind.Absolute))
            {
                throw new ArgumentException($"--endpoint '{Endpoint}' is not a valid URI.");
            }
        }
    }
}