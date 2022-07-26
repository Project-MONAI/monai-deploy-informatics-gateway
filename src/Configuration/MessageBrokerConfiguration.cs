/*
 * Copyright 2021-2022 MONAI Consortium
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

using Microsoft.Extensions.Configuration;
using Monai.Deploy.Messaging.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class MessageBrokerConfiguration : MessageBrokerServiceConfiguration
    {
        public static readonly string InformaticsGatewayApplicationId = "16988a78-87b5-4168-a5c3-2cfc2bab8e54";

        /// <summary>
        /// Gets or sets retry options relate to the message broker services.
        /// </summary>
        [ConfigurationKeyName("retries")]
        public RetryConfiguration Retries { get; set; } = new RetryConfiguration();

        /// <summary>
        /// Gets or sets the topics for events published/subscribed by Informatics Gateway
        /// </summary>
        [ConfigurationKeyName("topics")]
        public MessageBrokerConfigurationKeys Topics { get; set; } = new MessageBrokerConfigurationKeys();
    }
}
