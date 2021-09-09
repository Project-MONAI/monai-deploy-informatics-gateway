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

using Monai.Deploy.InformaticsGateway.Api;
using System;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    public enum ChangedEventType
    {
        Added,
        Updated,
        Deleted
    }

    public class MonaiApplicationentityChangedEvent
    {
        public MonaiApplicationEntity ApplicationEntity { get; }

        public ChangedEventType Event { get; }

        public MonaiApplicationentityChangedEvent(MonaiApplicationEntity applicationEntity, ChangedEventType eventType)
        {
            ApplicationEntity = applicationEntity ?? throw new ArgumentNullException(nameof(applicationEntity));
            Event = eventType;
        }
    }

    /// <summary>
    /// Interface for notifying any changes to configured MONAI Application Entities for MONAI SCP service.
    /// </summary>
    public interface IMonaiAeChangedNotificationService : IObservable<MonaiApplicationentityChangedEvent>
    {
        /// <summary>
        /// Notifies a new change.
        /// </summary>
        /// <param name="monaiApplicationChangedEvent">Change event</param>
        void Notify(MonaiApplicationentityChangedEvent monaiApplicationChangedEvent);
    }
}
