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

using System.Linq.Expressions;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Database.Api.Repositories
{
    public interface IPayloadRepository
    {
        Task<List<Payload>> ToListAsync(CancellationToken cancellationToken = default);

        Task<Payload> AddAsync(Payload item, CancellationToken cancellationToken = default);

        Task<Payload> UpdateAsync(Payload entity, CancellationToken cancellationToken = default);

        Task<Payload> RemoveAsync(Payload entity, CancellationToken cancellationToken = default);

        Task<bool> ContainsAsync(Expression<Func<Payload, bool>> predicate, CancellationToken cancellationToken = default);

        Task<int> RemovePendingPayloadsAsync(CancellationToken cancellationToken = default);

        Task<List<Payload>> GetPayloadsInStateAsync(CancellationToken cancellationToken = default, params Payload.PayloadState[] states);
    }
}
