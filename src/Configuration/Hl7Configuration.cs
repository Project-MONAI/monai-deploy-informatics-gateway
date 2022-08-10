// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
