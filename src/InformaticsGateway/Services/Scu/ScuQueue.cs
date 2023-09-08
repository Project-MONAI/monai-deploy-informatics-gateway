/*
 * Copyright 2022 MONAI Consortium
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

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Scu
{
    internal class ScuQueue : IScuQueue
    {
        private readonly BlockingCollection<ScuWorkRequest> _workItems;

        public ScuQueue(ILogger<ScuQueue> logger)
        {
            _workItems = new BlockingCollection<ScuWorkRequest>();
        }

        public ScuWorkRequest Dequeue(CancellationToken cancellationToken)
        {
            return _workItems.Take(cancellationToken);
        }

        public async Task<ScuWorkResponse> Queue(ScuWorkRequest request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));
            _workItems.Add(request, cancellationToken);

            return await request.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
