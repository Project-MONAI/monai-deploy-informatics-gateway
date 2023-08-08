using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Database.MongoDB.Configurations;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories
{
    public class RemoteAppExecutionRepository : IRemoteAppExecutionRepository, IDisposable
    {
        private readonly ILogger<RemoteAppExecutionRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IMongoCollection<RemoteAppExecution> _collection;
        private bool _disposedValue;

        public RemoteAppExecutionRepository(IServiceScopeFactory serviceScopeFactory,
            ILogger<RemoteAppExecutionRepository> logger,
            IOptions<InformaticsGatewayConfiguration> options,
            IOptions<MongoDBOptions> mongoDbOptions)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));
            Guard.Against.Null(mongoDbOptions, nameof(mongoDbOptions));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Database.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));

            var mongoDbClient = _scope.ServiceProvider.GetRequiredService<IMongoClient>();
            var mongoDatabase = mongoDbClient.GetDatabase(mongoDbOptions.Value.DaatabaseName);
            _collection = mongoDatabase.GetCollection<RemoteAppExecution>(nameof(RemoteAppExecution));
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var options = new CreateIndexOptions { Unique = true, ExpireAfter = TimeSpan.FromDays(7), Name = "RequestTime" };
            var indexDefinitionState = Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.OutgoingStudyUid);
            var indexModel = new CreateIndexModel<RemoteAppExecution>(indexDefinitionState, options);

            _collection.Indexes.CreateOne(indexModel);
        }

        public async Task<bool> AddAsync(RemoteAppExecution item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.InsertOneAsync(item, cancellationToken: cancellationToken).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);
        }

        public async Task<int> RemoveAsync(string OutgoingStudyUid, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var results = await _collection.DeleteManyAsync(Builders<RemoteAppExecution>.Filter.Where(p => p.OutgoingStudyUid == OutgoingStudyUid), cancellationToken).ConfigureAwait(false);
                return Convert.ToInt32(results.DeletedCount);
            }).ConfigureAwait(false);
        }

        public async Task<RemoteAppExecution?> GetAsync(string OutgoingStudyUid, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _collection.Find(p => p.OutgoingStudyUid == OutgoingStudyUid).FirstOrDefaultAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }


        public void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _scope.Dispose();
                }

                _disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
