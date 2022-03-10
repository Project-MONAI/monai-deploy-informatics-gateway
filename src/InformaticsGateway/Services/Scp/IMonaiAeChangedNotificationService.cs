// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using Monai.Deploy.InformaticsGateway.Api;

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
