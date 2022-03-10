// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
