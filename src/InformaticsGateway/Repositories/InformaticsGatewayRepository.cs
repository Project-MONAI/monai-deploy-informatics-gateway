// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Monai.Deploy.InformaticsGateway.Database;

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
            return await _informaticsGatewayContext.Set<T>().ToListAsync().ConfigureAwait(false);
        }

        public async Task<T> FindAsync(params object[] keyValues)
        {
            Guard.Against.Null(keyValues, nameof(keyValues));

            return await _informaticsGatewayContext.FindAsync<T>(keyValues).ConfigureAwait(false);
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
            return await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<EntityEntry<T>> AddAsync(T item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return await _informaticsGatewayContext.AddAsync(item, cancellationToken).ConfigureAwait(false);
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
