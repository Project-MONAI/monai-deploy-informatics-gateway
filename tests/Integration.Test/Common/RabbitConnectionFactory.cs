/*
 * Copyright 2022-2023 MONAI Consortium
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

using Monai.Deploy.InformaticsGateway.Configuration;
using RabbitMQ.Client;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    public static class RabbitConnectionFactory
    {
        private static IModel Channel { get; set; }

        public static IModel GetRabbitConnection(MessageBrokerConfiguration configuration)
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = configuration.PublisherSettings["endpoint"],
                UserName = configuration.PublisherSettings["username"],
                Password = configuration.PublisherSettings["password"],
                VirtualHost = configuration.PublisherSettings["virtualHost"],
            };

            Channel = connectionFactory.CreateConnection().CreateModel();

            return Channel;
        }

        public static void DeleteQueue(MessageBrokerConfiguration configuration, string queueName)
        {
            if (Channel is null)
            {
                GetRabbitConnection(configuration);
            }

            Channel?.QueueDelete(queueName);
        }

        public static void PurgeQueue(MessageBrokerConfiguration configuration, string queueName)
        {
            if (Channel is null || Channel.IsClosed)
            {
                GetRabbitConnection(configuration);
            }

            try
            {
                Channel?.QueuePurge(queueName);
            }
            catch (Exception)
            {
            }
        }

        public static void DeleteAllQueues(InformaticsGatewayConfiguration configuration)
        {
            DeleteQueue(configuration.Messaging, configuration.Messaging.Topics.WorkflowRequest);
            DeleteQueue(configuration.Messaging, configuration.Messaging.Topics.ExportComplete);
            DeleteQueue(configuration.Messaging, $"{configuration.Messaging.Topics.ExportRequestPrefix}.{configuration.Dicom.Scu.AgentName}");
            DeleteQueue(configuration.Messaging, $"{configuration.Messaging.Topics.ExportRequestPrefix}.{configuration.DicomWeb.AgentName}");
            DeleteQueue(configuration.Messaging, $"{configuration.Messaging.Topics.WorkflowRequest}-dead-letter");
            DeleteQueue(configuration.Messaging, $"{configuration.Messaging.Topics.ExportComplete}-dead-letter");
            DeleteQueue(configuration.Messaging, $"{configuration.Messaging.Topics.ExportRequestPrefix}.{configuration.Dicom.Scu.AgentName}-dead-letter");
            DeleteQueue(configuration.Messaging, $"{configuration.Messaging.Topics.ExportRequestPrefix}.{configuration.DicomWeb.AgentName}-dead-letter");
        }

        public static void PurgeAllQueues(MessageBrokerConfiguration configuration)
        {
            PurgeQueue(configuration, configuration.Topics.WorkflowRequest);
            PurgeQueue(configuration, configuration.Topics.ExportComplete);
            PurgeQueue(configuration, configuration.Topics.ExportRequestPrefix);
            PurgeQueue(configuration, $"{configuration.Topics.WorkflowRequest}-dead-letter");
            PurgeQueue(configuration, $"{configuration.Topics.ExportComplete}-dead-letter");
            PurgeQueue(configuration, $"{configuration.Topics.ExportRequestPrefix}-dead-letter");
        }
    }
}
