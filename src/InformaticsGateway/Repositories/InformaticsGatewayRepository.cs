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

using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Monai.Deploy.InformaticsGateway.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Repositories
{
    internal class InformaticsGatewayRepository<T> : IInformaticsGatewayRepository<T> where T : class
    {
        private readonly IServiceScope _scope;
        private readonly InformaticsGatewayContext _informaticsGatewayContext;

        public InformaticsGatewayRepository(IServiceScopeFactory serviceScopeFactory)
        {
            if (serviceScopeFactory is null)
            {
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            }

            _scope = serviceScopeFactory.CreateScope();
            _informaticsGatewayContext = _scope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>();
        }

        public IQueryable<T> AsQueryable()
        {
            return _informaticsGatewayContext.Set<T>().AsQueryable();
        }

        public async Task<List<T>> ToListAsync()
        {
            return await _informaticsGatewayContext.Set<T>().ToListAsync();
        }

        public async Task<T> FindAsync(params object[] keyValues)
        {
            Guard.Against.Null(keyValues, nameof(keyValues));

            return await _informaticsGatewayContext.FindAsync<T>(keyValues);
        }

        public EntityEntry<T> Update(T entity)
        {
            Guard.Against.Null(entity, nameof(entity));

            return _informaticsGatewayContext.Update<T>(entity);
        }

        public EntityEntry<T> Remove(T entity)
        {
            Guard.Against.Null(entity, nameof(entity));

            return _informaticsGatewayContext.Remove<T>(entity);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _informaticsGatewayContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<EntityEntry<T>> AddAsync(T item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return await _informaticsGatewayContext.AddAsync(item, cancellationToken);
        }

        public T FirstOrDefault(Func<T, bool> func)
        {
            Guard.Against.Null(func, nameof(func));

            return _informaticsGatewayContext.Set<T>().FirstOrDefault(func);
        }

        public void Detach(T item)
        {
            Guard.Against.Null(item, nameof(item));
            _informaticsGatewayContext.Entry(item).State = EntityState.Detached;
        }

        public bool Any(Func<T, bool> func)
        {
            Guard.Against.Null(func, nameof(func));
            return _informaticsGatewayContext.Set<T>().Any(func);
        }
    }
}
