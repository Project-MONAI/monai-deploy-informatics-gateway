// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Moq;
using xRetry;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scp
{
    public class MonaiAeChangedNotificationServiceTest
    {
        private readonly Mock<ILogger<MonaiAeChangedNotificationService>> _logger;

        public MonaiAeChangedNotificationServiceTest()
        {
            _logger = new Mock<ILogger<MonaiAeChangedNotificationService>>();
        }

        [RetryFact(5, 250, DisplayName = "Workflow Test")]
        public void WorkflowTest()
        {
            var service = new MonaiAeChangedNotificationService(_logger.Object);
            var observer = new Mock<IObserver<MonaiApplicationentityChangedEvent>>();
            observer.Setup(p => p.OnNext(It.IsAny<MonaiApplicationentityChangedEvent>()));

            var cancel = service.Subscribe(observer.Object);
            service.Notify(new MonaiApplicationentityChangedEvent(new MonaiApplicationEntity(), ChangedEventType.Added));
            service.Notify(new MonaiApplicationentityChangedEvent(new MonaiApplicationEntity(), ChangedEventType.Deleted));
            service.Notify(new MonaiApplicationentityChangedEvent(new MonaiApplicationEntity(), ChangedEventType.Updated));

            observer.Verify(p => p.OnNext(It.IsAny<MonaiApplicationentityChangedEvent>()), Times.Exactly(3));

            cancel.Dispose();
            observer.Reset();
            service.Notify(new MonaiApplicationentityChangedEvent(new MonaiApplicationEntity(), ChangedEventType.Updated));
            observer.Verify(p => p.OnNext(It.IsAny<MonaiApplicationentityChangedEvent>()), Times.Never());
        }

        [RetryFact(5, 250, DisplayName = "Shall log when subscriber throws")]
        public void ShallLogWhenSubscriberThrows()
        {
            var service = new MonaiAeChangedNotificationService(_logger.Object);
            var observer = new Mock<IObserver<MonaiApplicationentityChangedEvent>>();
            observer.Setup(p => p.OnNext(It.IsAny<MonaiApplicationentityChangedEvent>())).Throws(new Exception());
            observer.Setup(p => p.OnError(It.IsAny<Exception>()));

            var cancel = service.Subscribe(observer.Object);
            service.Notify(new MonaiApplicationentityChangedEvent(new MonaiApplicationEntity(), ChangedEventType.Added));
            observer.Verify(p => p.OnNext(It.IsAny<MonaiApplicationentityChangedEvent>()), Times.Once());
            observer.Verify(p => p.OnError(It.IsAny<Exception>()), Times.Once());

            observer.Reset();
            cancel.Dispose();
            observer.Verify(p => p.OnNext(It.IsAny<MonaiApplicationentityChangedEvent>()), Times.Never());
        }
    }
}
