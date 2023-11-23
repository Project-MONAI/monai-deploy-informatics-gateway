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
    public interface IHL7DestinationEntityRepository
    {
        Task<List<HL7DestinationEntity>> ToListAsync(CancellationToken cancellationToken = default);

        Task<HL7DestinationEntity?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<HL7DestinationEntity> AddAsync(HL7DestinationEntity item, CancellationToken cancellationToken = default);

        Task<HL7DestinationEntity> UpdateAsync(HL7DestinationEntity entity, CancellationToken cancellationToken = default);

        Task<HL7DestinationEntity> RemoveAsync(HL7DestinationEntity entity, CancellationToken cancellationToken = default);

        Task<bool> ContainsAsync(Expression<Func<HL7DestinationEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
