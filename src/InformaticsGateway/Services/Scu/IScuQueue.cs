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

using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Scu
{
    public enum RequestType
    {
        CEcho,
    }

    public interface IScuQueue
    {
        /// <summary>
        /// Queue a new ScuRequest for the SCU Service.
        /// </summary>
        /// <param name="request">SCU Request for the SCU Service.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        Task<ScuResponse> Queue(ScuRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Dequeue a ScuRequest from the queue for processing.
        /// The default implementation blocks the call until a file is available from the queue.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        ScuRequest Dequeue(CancellationToken cancellationToken);
    }
}
