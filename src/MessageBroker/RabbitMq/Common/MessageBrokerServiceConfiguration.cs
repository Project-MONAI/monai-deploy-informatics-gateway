// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.MessageBroker.Common
{
    public abstract class MessageBrokerServiceConfiguration
    {
        /// <summary>
        /// Gets or sets the a fully qualified type name of the message publisher service.
        /// The spcified type must implement <typeparam name="Monai.Deploy.InformaticsGateway.Api.MessageBroker.IMessageBrokerPublisherService">IMessageBrokerPublisherService</typeparam> interface.
        /// The default message publisher service configured is RabbitMQ.
        /// </summary>
        [ConfigurationKeyName("publisherServiceAssemblyName")]
        public string PublisherServiceAssemblyName { get; set; } = "Monai.Deploy.MessageBroker.RabbitMq.RabbitMqMessagePublisherService, Monai.Deploy.MessageBroker";

        /// <summary>
        /// Gets or sets the a fully qualified type name of the message subscriber service.
        /// The spcified type must implement <typeparam name="Monai.Deploy.InformaticsGateway.Api.MessageBroker.IMessageBrokerSubscriberService">IMessageBrokerSubscriberService</typeparam> interface.
        /// The default message subscriber service configured is RabbitMQ.
        /// </summary>
        [ConfigurationKeyName("subscriberServiceAssemblyName")]
        public string SubscriberServiceAssemblyName { get; set; } = "Monai.Deploy.MessageBroker.RabbitMq.RabbitMqMessageSubscriberService, Monai.Deploy.MessageBroker";

        /// <summary>
        /// Gets or sets the message publisher specific settings.
        /// Service implementer shall validate settings in the constructor and specify all settings in a single level JSON object as in the example below.
        /// </summary>
        /// <example>
        /// <code>
        /// {
        ///     ...
        ///     "publisherSettings": {
        ///         "endpoint": "1.2.3.4",
        ///         "username": "monaideploy",
        ///         "password": "mysecret",
        ///         "setting-a": "value-a",
        ///         "setting-b": "value-b"
        ///     }
        /// }
        /// </code>
        /// </example>
        [ConfigurationKeyName("publisherSettings")]
        public Dictionary<string, string> PublisherSettings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the message subscriber specific settings.
        /// Service implementer shall validate settings in the constructor and specify all settings in a single level JSON object as in the example below.
        /// </summary>
        /// <example>
        /// <code>
        /// {
        ///     ...
        ///     "subscriberSettings": {
        ///         "endpoint": "1.2.3.4",
        ///         "username": "monaideploy",
        ///         "password": "myothersecret",
        ///         "setting-a": "value-a",
        ///         "setting-b": "value-b"
        ///     }
        /// }
        /// </code>
        /// </example>
        [ConfigurationKeyName("subscriberSettings")]
        public Dictionary<string, string> SubscriberSettings { get; set; } = new Dictionary<string, string>();
    }
}
