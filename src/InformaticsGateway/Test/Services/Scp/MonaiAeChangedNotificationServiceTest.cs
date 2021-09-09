// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using System;
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

            var cancel = service.Subscribe(observer.Object);
            service.Notify(new MonaiApplicationentityChangedEvent(new MonaiApplicationEntity(), ChangedEventType.Added));
            observer.Verify(p => p.OnNext(It.IsAny<MonaiApplicationentityChangedEvent>()), Times.Once());
            _logger.VerifyLogging("Error notifying observer.", LogLevel.Error, Times.Once());

            observer.Reset();
            cancel.Dispose();
            observer.Verify(p => p.OnNext(It.IsAny<MonaiApplicationentityChangedEvent>()), Times.Never());
        }
    }
}
