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

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class Hl7Configuration
    {
        public static readonly int DefaultClientTimeout = 60000;
        public const int DefaultMaximumNumberOfConnections = 10;

        /// <summary>
        /// Gets or sets the client connection timeout in milliseconds.
        /// Defaults to 60,000ms.
        /// </summary>
        [ConfigurationKeyName("clientTimeout")]
        public int ClientTimeoutMilliseconds { get; set; } = DefaultClientTimeout;

        /// <summary>
        /// Gets or sets maximum number of concurrent connections for the HL7 service.
        /// Defaults to 10.
        /// </summary>
        [ConfigurationKeyName("maximumNumberOfConnections")]
        public int MaximumNumberOfConnections { get; set; } = DefaultMaximumNumberOfConnections;

        /// <summary>
        /// Gets or sets the MLLP listening port.
        /// Defaults to 2575.
        /// </summary>
        [ConfigurationKeyName("port")]
        public int Port { get; set; } = 2575;

        /// <summary>
        /// Gets or sets wether to respond with an ack/nack message.
        /// Defaults to true.
        /// </summary>
        [ConfigurationKeyName("sendAck")]
        public bool SendAcknowledgment { get; set; } = true;

        public uint BufferSize { get; set; } = 10240;

        public Hl7Configuration()
        {
        }
    }
}
