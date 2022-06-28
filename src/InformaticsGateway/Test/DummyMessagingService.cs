// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Monai.Deploy.Messaging;
using Monai.Deploy.Messaging.API;
using Monai.Deploy.Messaging.Common;
using Monai.Deploy.Messaging.Messages;

namespace Monai.Deploy.InformaticsGateway.Test
{
    internal class DummyMessagePublisherRegistrar : PublisherServiceRegistrationBase
    {
        public DummyMessagePublisherRegistrar(string fullyQualifiedAssemblyName) : base(fullyQualifiedAssemblyName)
        {
        }

        public override IServiceCollection Configure(IServiceCollection services) => services;
    }
    internal class DummyMessageSubscriberRegistrar : SubscriberServiceRegistrationBase
    {
        public DummyMessageSubscriberRegistrar(string fullyQualifiedAssemblyName) : base(fullyQualifiedAssemblyName)
        {
        }

        public override IServiceCollection Configure(IServiceCollection services) => services;
    }

    internal class DummyMessagingService : IMessageBrokerPublisherService, IMessageBrokerSubscriberService
    {
        public string Name => "Dummy Messaging Service";

        public void Acknowledge(MessageBase message) => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
        public Task Publish(string topic, Message message) => throw new NotImplementedException();
        public void Reject(MessageBase message, bool requeue = true) => throw new NotImplementedException();
        public void Subscribe(string topic, string queue, Action<MessageReceivedEventArgs> messageReceivedCallback, ushort prefetchCount = 0) => throw new NotImplementedException();
        public void Subscribe(string[] topics, string queue, Action<MessageReceivedEventArgs> messageReceivedCallback, ushort prefetchCount = 0) => throw new NotImplementedException();
        public void SubscribeAsync(string topic, string queue, Func<MessageReceivedEventArgs, Task> messageReceivedCallback, ushort prefetchCount = 0) => throw new NotImplementedException();
        public void SubscribeAsync(string[] topics, string queue, Func<MessageReceivedEventArgs, Task> messageReceivedCallback, ushort prefetchCount = 0) => throw new NotImplementedException();
    }
}
