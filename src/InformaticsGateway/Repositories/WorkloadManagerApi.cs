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

using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Repositories
{
    internal class WorkloadManagerApi : IWorkloadManagerApi
    {
        public Task<byte[]> Download(string applicaton, Guid fileId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<TaskResponse>> GetPendingJobs(string agent, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(default(IList<TaskResponse>));
        }

        public Task ReportFailure(Guid taskId, bool retryLater, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ReportSuccess(Guid taskId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task Upload(FileStorageInfo file, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
