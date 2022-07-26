/*
 * Copyright 2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
