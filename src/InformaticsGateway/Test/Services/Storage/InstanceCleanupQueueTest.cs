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
using System.Threading;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Storage
{
    public class InstanceCleanupQueueTest
    {
        private readonly Mock<ILogger<InstanceCleanupQueue>> _logger;
        private readonly InstanceCleanupQueue _queue;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public InstanceCleanupQueueTest()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = new Mock<ILogger<InstanceCleanupQueue>>();
            _queue = new InstanceCleanupQueue(_logger.Object);
        }

        [RetryFact(5, 250, DisplayName = "Queue - Shall throw if null")]
        public void Queue_ShallThrowOnNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                _queue.Queue(null);
            });
        }

        [RetryFact(5, 250, DisplayName = "Shall queue and dequeue items")]
        public void ShallQueueAndDequeueItems()
        {
            for (var i = 0; i < 10; i++)
            {
                _queue.Queue(new TestStorageInfo("test"));
            }

            _cancellationTokenSource.CancelAfter(500);
            var items = new List<FileStorageInfo>();
            for (var i = 0; i < 10; i++)
            {
                items.Add(_queue.Dequeue(_cancellationTokenSource.Token));
            }

            Assert.Equal(10, items.Count);
        }
    }
}
