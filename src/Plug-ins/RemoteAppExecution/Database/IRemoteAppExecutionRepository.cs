/*
 * Copyright 2023 MONAI Consortium
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

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database
{
    public interface IRemoteAppExecutionRepository
    {
        Task<bool> AddAsync(RemoteAppExecution item, CancellationToken cancellationToken = default);

        Task<RemoteAppExecution?> GetAsync(string sopInstanceUid, CancellationToken cancellationToken = default);

        Task<RemoteAppExecution?> GetAsync(string workflowInstanceId, string exportTaskId, string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default);

        Task<RemoteAppExecution> RemoveAsync(RemoteAppExecution remoteAppExecution, CancellationToken cancellationToken = default);
    }
}
