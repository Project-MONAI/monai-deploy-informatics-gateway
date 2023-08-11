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

using System.Linq.Expressions;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database.Api.Repositories
{
    public interface IVirtualApplicationEntityRepository
    {
        Task<List<VirtualApplicationEntity>> ToListAsync(CancellationToken cancellationToken = default);

        Task<VirtualApplicationEntity?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<VirtualApplicationEntity?> FindByAeTitleAsync(string name, CancellationToken cancellationToken = default);

        Task<VirtualApplicationEntity> AddAsync(VirtualApplicationEntity item, CancellationToken cancellationToken = default);

        Task<VirtualApplicationEntity> UpdateAsync(VirtualApplicationEntity entity, CancellationToken cancellationToken = default);

        Task<VirtualApplicationEntity> RemoveAsync(VirtualApplicationEntity entity, CancellationToken cancellationToken = default);

        Task<bool> ContainsAsync(Expression<Func<VirtualApplicationEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
