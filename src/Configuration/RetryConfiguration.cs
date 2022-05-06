// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class RetryConfiguration
    {
        /// <summary>
        /// Gets or sets delays, in milliseconds, between each retry.
        /// Number of items specified is the number of times the call would be retried.
        /// Values can be separated by commas.
        /// Default is 250, 500, 1000.
        /// </summary>
        [ConfigurationKeyName("delays")]
        public int[] DelaysMilliseconds { get; set; } = new[] { 250, 500, 1000 };

        // Gets the delays in TimeSpan objects
        public IEnumerable<TimeSpan> RetryDelays
        {
            get
            {
                foreach (var retry in DelaysMilliseconds)
                {
                    yield return TimeSpan.FromMilliseconds(retry);
                }
            }
        }
    }
}
