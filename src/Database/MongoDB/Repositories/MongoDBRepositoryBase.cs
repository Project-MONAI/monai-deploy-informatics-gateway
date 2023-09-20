/*
 * Copyright 2021-2023 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using MongoDB.Driver;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories
{
    public abstract class MongoDBRepositoryBase
    {
        /// <summary>
        /// Get All T that match filters provided.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Collection to run against.</param>
        /// <param name="filterFunction">Filter function you can filter on properties of T.</param>
        /// <param name="sortFunction">Function used to sort data.</param>
        /// <param name="skip">Items to skip.</param>
        /// <param name="limit">Items to limit results by.</param>
        /// <returns></returns>
        protected static async Task<IList<T>> GetAllAsync<T>(IMongoCollection<T> collection,
            Expression<Func<T, bool>>? filterFunction,
            SortDefinition<T> sortFunction,
            int? skip = null,
            int? limit = null)
        {
            return await collection
                .Find(filterFunction)
                .Skip(skip)
                .Limit(limit)
                .Sort(sortFunction)
                .ToListAsync().ConfigureAwait(false);
        }

        protected static async Task<IList<T>> GetAllAsync<T>(IMongoCollection<T> collection,
            FilterDefinition<T> filterFunction,
            SortDefinition<T> sortFunction,
            int? skip = null,
            int? limit = null)
        {
            var result = await collection
                .Find(filterFunction)
                .Skip(skip)
                .Limit(limit)
                .Sort(sortFunction)
                .ToListAsync().ConfigureAwait(false);
            return result;
        }
    }
}
