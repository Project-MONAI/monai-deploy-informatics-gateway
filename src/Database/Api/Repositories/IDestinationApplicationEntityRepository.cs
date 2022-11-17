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
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database.Api.Repositories
{
    public interface IDestinationApplicationEntityRepository
    {
        Task<List<DestinationApplicationEntity>> ToListAsync(CancellationToken cancellationToken = default);

        Task<DestinationApplicationEntity?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<DestinationApplicationEntity> AddAsync(DestinationApplicationEntity item, CancellationToken cancellationToken = default);

        Task<DestinationApplicationEntity> UpdateAsync(DestinationApplicationEntity entity, CancellationToken cancellationToken = default);

        Task<DestinationApplicationEntity> RemoveAsync(DestinationApplicationEntity entity, CancellationToken cancellationToken = default);

        Task<bool> ContainsAsync(Expression<Func<DestinationApplicationEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
