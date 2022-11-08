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

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Monai.Deploy.InformaticsGateway.Database.Api
{
    public interface IInformaticsGatewayRepository<T> where T : class
    {
        IQueryable<T> AsQueryable();

        Task<T> FindAsync(params object[] keyValues);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<List<T>> ToListAsync();

        EntityEntry<T> Update(T entity);

        EntityEntry<T> Remove(T entity);

        void RemoveRange(params T[] entities);

        Task<EntityEntry<T>> AddAsync(T item, CancellationToken cancellationToken = default);

        T FirstOrDefault(Func<T, bool> p);

        void Detach(T job);

        bool Any(Func<T, bool> p);
    }
}
