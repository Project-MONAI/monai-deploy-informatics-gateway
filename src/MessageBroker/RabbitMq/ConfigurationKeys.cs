// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Monai.Deploy.InformaticsGateway.MessageBroker.RabbitMq
{
    internal static class ConfigurationKeys
    {
        public static readonly string EndPoint = "endpoint";
        public static readonly string Username = "username";
        public static readonly string Password = "password";
        public static readonly string VirtualHost = "virtualHost";
        public static readonly string Exchange = "exchange";
        public static readonly string ExportRequestQueue = "exportRequestQueue";

        public static readonly string[] PublisherRequiredKeys = new[] { EndPoint, Username, Password, VirtualHost, Exchange };
        public static readonly string[] SubscriberRequiredKeys = new[] { EndPoint, Username, Password, VirtualHost, Exchange, ExportRequestQueue };
    }
}
