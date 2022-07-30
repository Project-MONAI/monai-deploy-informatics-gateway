/*
 * Copyright 2021-2022 MONAI Consortium
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database;
using Monai.Deploy.InformaticsGateway.Logging;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Repositories
{
    public class StorageMetadataWrapperRepository : IStorageMetadataWrapperRepository
    {
        private readonly ILogger<StorageMetadataWrapperRepository> _logger;
        private readonly IInformaticsGatewayRepository<StorageMetadataWrapper> _repository;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public StorageMetadataWrapperRepository(
            ILogger<StorageMetadataWrapperRepository> logger,
            IInformaticsGatewayRepository<StorageMetadataWrapper> repository,
            IOptions<InformaticsGatewayConfiguration> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task AddAsync(FileStorageMetadata metadata)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "Correlation ID", metadata.CorrelationId }, { "Identity", metadata.Id } });
            await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    _options.Value.Database.Retries.RetryDelays,
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.ErrorSavingFileStorageMetadata(timeSpan, retryCount, exception);
                    })
                .ExecuteAsync(async () =>
                {
                    var obj = new StorageMetadataWrapper(metadata);
                    await _repository.AddAsync(obj).ConfigureAwait(false);
                    await _repository.SaveChangesAsync().ConfigureAwait(false);
                    _repository.Detach(obj);
                    _logger.StorageMetadataSaved();
                })
                .ConfigureAwait(false);
        }

        public async Task UpdateAsync(FileStorageMetadata metadata)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "Correlation ID", metadata.CorrelationId }, { "Identity", metadata.Id } });
            await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    _options.Value.Database.Retries.RetryDelays,
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.ErrorSavingFileStorageMetadata(timeSpan, retryCount, exception);
                    })
                .ExecuteAsync(async () =>
                {
                    var @object = _repository.FirstOrDefault(p => p.Identity == metadata.Id && p.CorrelationId == metadata.CorrelationId);

                    if (@object is null)
                    {
                        throw new ArgumentException("Matching wrapper storage object not found");
                    }

                    @object.Update(metadata);
                    _repository.Update(@object);
                    await _repository.SaveChangesAsync().ConfigureAwait(false);
                    _repository.Detach(@object);

                    _logger.StorageMetadataSaved();
                })
                .ConfigureAwait(false);
        }

        public async Task AddOrUpdateAsync(FileStorageMetadata metadata)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            var existing = GetFileStorageMetdadata(metadata.CorrelationId, metadata.Id);

            if (existing is not null)
            {
                await UpdateAsync(metadata).ConfigureAwait(false);
            }
            else
            {
                await AddAsync(metadata).ConfigureAwait(false);
            }
        }

        public IList<FileStorageMetadata> GetFileStorageMetdadata(string correlationId)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));

            return _repository.AsQueryable().Where(p => p.CorrelationId == correlationId)
                    .Select(p => p.GetObject())
                    .ToList();
        }

        public FileStorageMetadata GetFileStorageMetdadata(string correlationId, string identity)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrWhiteSpace(identity, nameof(identity));

            return _repository.FirstOrDefault(p => p.CorrelationId.Equals(correlationId, StringComparison.Ordinal) && p.Identity.Equals(identity, StringComparison.Ordinal))?.GetObject();
        }

        public async Task<bool> DeleteAsync(string correlationId, string identity)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrWhiteSpace(identity, nameof(identity));

            var toBeDeleted = _repository.FirstOrDefault(p => p.CorrelationId.Equals(correlationId, StringComparison.Ordinal) && p.Identity.Equals(identity, StringComparison.Ordinal));

            if (toBeDeleted is not null)
            {
                _repository.Remove(toBeDeleted);
                await _repository.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            return false;
        }

        public async Task DeletePendingUploadsAsync()
        {
            await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    _options.Value.Database.Retries.RetryDelays,
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.ErrorDeletingPendingUploads(timeSpan, retryCount, exception);
                    })
                .ExecuteAsync(async () =>
                {
                    var toBeDeleted = _repository.AsQueryable().Where(p => !p.IsUploaded);

                    if (toBeDeleted.Any())
                    {
                        _repository.RemoveRange(toBeDeleted.ToArray());
                        await _repository.SaveChangesAsync().ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
        }
    }
}
