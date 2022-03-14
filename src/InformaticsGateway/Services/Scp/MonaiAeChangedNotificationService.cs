// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Common;

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

        public void Notify(MonaiApplicationentityChangedEvent monaiApplicationChangedEvent)
        {
            Guard.Against.Null(monaiApplicationChangedEvent, nameof(monaiApplicationChangedEvent));

            _logger.Log(LogLevel.Information, $"Notifying {_observers.Count} observers of MONAI Application Entity {monaiApplicationChangedEvent.Event}.");

            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnNext(monaiApplicationChangedEvent);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            }
        }
    }
}
