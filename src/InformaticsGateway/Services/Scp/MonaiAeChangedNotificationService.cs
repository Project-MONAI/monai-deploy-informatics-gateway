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

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Common;
using System;
using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    /// <inheritdoc/>
    public sealed class MonaiAeChangedNotificationService : IMonaiAeChangedNotificationService
    {
        private readonly ILogger<MonaiAeChangedNotificationService> _logger;
        private readonly IList<IObserver<MonaiApplicationentityChangedEvent>> _observers;

        public MonaiAeChangedNotificationService(ILogger<MonaiAeChangedNotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _observers = new List<IObserver<MonaiApplicationentityChangedEvent>>();
        }

        public IDisposable Subscribe(IObserver<MonaiApplicationentityChangedEvent> observer)
        {
            Guard.Against.Null(observer, nameof(observer));

            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new Unsubscriber<MonaiApplicationentityChangedEvent>(_observers, observer);
        }

        public void Notify(MonaiApplicationentityChangedEvent applicationChangedEvent)
        {
            Guard.Against.Null(applicationChangedEvent, nameof(applicationChangedEvent));

            _logger.Log(LogLevel.Information, $"Notifying {_observers.Count} observers of MONAI Application Entity {applicationChangedEvent.Event}.");

            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnNext(applicationChangedEvent);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            }
        }
    }
}
