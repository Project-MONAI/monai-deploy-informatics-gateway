// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Storage
{
    public class MinIoStorageService : IStorageService
    {
        private readonly ILogger<MinIoStorageService> _logger;
        private readonly MinioClient _client;

        public string Name => "MinIO Storage Service";

        public MinIoStorageService(IOptions<InformaticsGatewayConfiguration> options, ILogger<MinIoStorageService> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var configuration = options.Value.Storage;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = new MinioClient(configuration.StorageServiceCredentials.Endpoint, configuration.StorageServiceCredentials.AccessKey, configuration.StorageServiceCredentials.AccessToken);

            if (configuration.SecuredConnection)
            {
                _client.WithSSL();
            }
        }

        public async Task CopyObject(string sourceBucketName, string sourceObjectName, string destinationBucketName, string destinationObjectName, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(sourceBucketName, nameof(sourceBucketName));
            Guard.Against.NullOrWhiteSpace(sourceObjectName, nameof(sourceObjectName));
            Guard.Against.NullOrWhiteSpace(destinationBucketName, nameof(destinationBucketName));
            Guard.Against.NullOrWhiteSpace(destinationObjectName, nameof(destinationObjectName));

            await _client.CopyObjectAsync(sourceBucketName, sourceObjectName, destinationBucketName, destinationObjectName, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task GetObject(string bucketName, string objectName, Action<Stream> callback, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));
            Guard.Against.NullOrWhiteSpace(objectName, nameof(objectName));
            Guard.Against.Null(callback, nameof(callback));

            await _client.GetObjectAsync(bucketName, objectName, callback, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public IList<VirtualFileInfo> ListObjects(string bucketName, string? prefix = "", bool recursive = false, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));

            var files = new List<VirtualFileInfo>();
            var objservable = _client.ListObjectsAsync(bucketName, prefix, recursive, cancellationToken);
            var completedEvent = new ManualResetEventSlim(false);
            objservable.Subscribe<Minio.DataModel.Item>(item =>
            {
                if (!item.IsDir)
                {
                    files.Add(new VirtualFileInfo
                    {
                        FilePath = item.Key,
                        Filename = Path.GetFileName(item.Key),
                        ETag = item.ETag,
                        Size = item.Size,
                        LastModifiedDateTime = item.LastModifiedDateTime
                    });
                }
            },
            error =>
            {
                _logger.Log(LogLevel.Warning, error, $"Error listing objects in bucket '{bucketName}'.");
            },
            () => completedEvent.Set(), cancellationToken);

            completedEvent.Wait(cancellationToken);
            return files;
        }

        public async Task PutObject(string bucketName, string objectName, Stream data, long size, string contentType, Dictionary<string, string> metadata, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));
            Guard.Against.NullOrWhiteSpace(objectName, nameof(objectName));
            Guard.Against.Null(data, nameof(data));
            Guard.Against.NullOrWhiteSpace(contentType, nameof(contentType));

            await _client.PutObjectAsync(bucketName, objectName, data, size, contentType, metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveObject(string bucketName, string objectName, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));
            Guard.Against.NullOrWhiteSpace(objectName, nameof(objectName));

            await _client.RemoveObjectAsync(bucketName, objectName, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveObjects(string bucketName, IEnumerable<string> objectNames, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));
            Guard.Against.NullOrEmpty(objectNames, nameof(objectNames));

            await _client.RemoveObjectAsync(bucketName, objectNames, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
