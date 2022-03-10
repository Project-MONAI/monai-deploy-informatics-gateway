// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Monai.Deploy.InformaticsGateway.Repositories
{
    public interface IInformaticsGatewayRepository<T> where T : class
    {
        IQueryable<T> AsQueryable();

        Task<T> FindAsync(params object[] keyValues);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<List<T>> ToListAsync();

        EntityEntry<T> Update(T entity);

        EntityEntry<T> Remove(T entity);

        Task<EntityEntry<T>> AddAsync(T item, CancellationToken cancellationToken = default);

        T FirstOrDefault(Func<T, bool> p);

        void Detach(T job);

        bool Any(Func<T, bool> p);
    }
}
