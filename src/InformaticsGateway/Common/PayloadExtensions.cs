// Copyright 2021-2022 MONAI Consortium
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
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Repositories;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                       logger.Log(LogLevel.Error, exception, $"Error saving payload. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
                   })
               .ExecuteAsync(async () =>
               {
                   await repository.SaveChangesAsync();
                   logger.Log(LogLevel.Debug, $"Payload {payload.Id} saved.");
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
                       logger.Log(LogLevel.Error, exception, $"Error adding payload. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
                   })
               .ExecuteAsync(async () =>
               {
                   await repository.AddAsync(payload);
                   await repository.SaveChangesAsync();
                   logger.Log(LogLevel.Debug, $"Payload {payload.Id} added.");
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
                       logger.Log(LogLevel.Error, exception, $"Error deleting payload. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
                   })
               .ExecuteAsync(async () =>
               {
                   repository.Remove(payload);
                   await repository.SaveChangesAsync();
                   logger.Log(LogLevel.Debug, $"Payload {payload.Id} deleted.");
               })
               .ConfigureAwait(false);
        }
    }
}
