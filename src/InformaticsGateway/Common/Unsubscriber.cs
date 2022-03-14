// SPDX-FileCopyrightText: � 2022 MONAI Consortium
// SPDX-FileCopyrightText: � 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.Common
{
    /// <summary>
    /// Unsubscriber<T> class is used with IObserver<T> for unsubscribing from a notification service.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class Unsubscriber<T> : IDisposable
    {
        private readonly IList<IObserver<T>> _observers;
        private readonly IObserver<T> _observer;
        private bool _disposedValue;

        internal Unsubscriber(IList<IObserver<T>> observers, IObserver<T> observer)
        {
            _observers = observers ?? throw new ArgumentNullException(nameof(observers));
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && _observers.Contains(_observer))
                {
                    _observers.Remove(_observer);
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
