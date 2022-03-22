// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class ServiceCredentials
    {
        [JsonProperty(PropertyName = "endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty(PropertyName = "accessKey")]
        public string AccessKey { get; set; }

        [JsonProperty(PropertyName = "accessToken")]
        public string AccessToken { get; set; }
    }
}
