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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Logging;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Common
{
    internal static class PayloadExtensions
    {
        public static async Task UpdatePayload(this Payload payload, IEnumerable<TimeSpan> retryDelays, ILogger logger, IInformaticsGatewayRepository<Payload> repository)
        {
            Guard.Against.Null(payload, nameof(payload));
            Guard.Against.NullOrEmpty(retryDelays, nameof(retryDelays));
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(repository, nameof(repository));

            await Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(
                   retryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       logger.ErrorSavingPayload(timeSpan, retryCount, exception);
                   })
               .ExecuteAsync(async () =>
               {
                   await repository.SaveChangesAsync().ConfigureAwait(false);
                   logger.PayloadSaved(payload.Id);
               })
               .ConfigureAwait(false);
        }

        public static async Task AddPayaloadToDatabase(this Payload payload, IEnumerable<TimeSpan> retryDelays, ILogger logger, IInformaticsGatewayRepository<Payload> repository)
        {
            Guard.Against.Null(payload, nameof(payload));
            Guard.Against.NullOrEmpty(retryDelays, nameof(retryDelays));
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(repository, nameof(repository));
            await Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(
                   retryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       logger.ErrorAddingPayload(timeSpan, retryCount, exception);
                   })
               .ExecuteAsync(async () =>
               {
                   await repository.AddAsync(payload).ConfigureAwait(false);
                   await repository.SaveChangesAsync().ConfigureAwait(false);
                   logger.PayloadAdded(payload.Id);
               })
               .ConfigureAwait(false);
        }

        public static async Task DeletePayload(this Payload payload, IEnumerable<TimeSpan> retryDelays, ILogger logger, IInformaticsGatewayRepository<Payload> repository)
        {
            Guard.Against.Null(payload, nameof(payload));
            Guard.Against.NullOrEmpty(retryDelays, nameof(retryDelays));
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(repository, nameof(repository));

            await Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(
                   retryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       logger.ErrorDeletingPayload(timeSpan, retryCount, exception);
                   })
               .ExecuteAsync(async () =>
               {
                   repository.Remove(payload);
                   await repository.SaveChangesAsync().ConfigureAwait(false);
                   logger.PayloadDeleted(payload.Id);
               })
               .ConfigureAwait(false);
        }
    }
}
