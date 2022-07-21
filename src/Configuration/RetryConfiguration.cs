/*
 * Copyright 2021-2022 MONAI Consortium
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
