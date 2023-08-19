/*
 * Copyright 2022-2023 MONAI Consortium
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

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test;
using Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories;
using MongoDB.Driver;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Integration.Test
{
    [Collection("MongoDatabase")]
    public class StorageMetadataWrapperRepositoryTest
    {
        private readonly MongoDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<StorageMetadataWrapperRepository>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public StorageMetadataWrapperRepositoryTest(MongoDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<StorageMetadataWrapperRepository>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => databaseFixture.Client);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Database.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void GivenStorageMetadataWrapperRepositoryType_WhenInitialized_TheConstructorShallGuardAllParameters()
        {
            Assert.Throws<ArgumentNullException>(() => new StorageMetadataWrapperRepository(null!, null!, null!, null!));
            Assert.Throws<ArgumentNullException>(() => new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, null!, null!, null!));
            Assert.Throws<ArgumentNullException>(() => new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, null!, null!));
            Assert.Throws<ArgumentNullException>(() => new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, null!));

            _ = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
        }

        [Fact]
        public async Task GivenADicomStorageMetadataObject_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var metadata = CreateMetadataObject();

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
            await store.AddAsync(metadata).ConfigureAwait(false);

            var collection = _databaseFixture.Database.GetCollection<StorageMetadataWrapper>(nameof(StorageMetadataWrapper));
            var actual = await collection.Find(p => p.Identity == metadata.Id).FirstOrDefaultAsync().ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(metadata.CorrelationId, actual!.CorrelationId);
            Assert.Equal(metadata.IsUploaded, actual!.IsUploaded);
            Assert.Equal(metadata.GetType().AssemblyQualifiedName, actual!.TypeName);
            Assert.Equal(JsonSerializer.Serialize<object>(metadata), actual!.Value);
        }

        [Fact]
        public async Task GivenANonExistedDicomStorageMetadataObject_WhenSavedToDatabase_ThrowsArgumentException()
        {
            var metadata1 = CreateMetadataObject();
            var metadata2 = CreateMetadataObject();

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);

            await store.AddOrUpdateAsync(metadata1).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentException>(async () => await store.UpdateAsync(metadata2).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task GivenAnExistingDicomStorageMetadataObject_WhenUpdated_ExpectItToBeSaved()
        {
            var metadata = CreateMetadataObject();

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
            await store.AddAsync(metadata).ConfigureAwait(false);
            metadata.SetWorkflows("A", "B", "C");
            metadata.File.SetUploaded("bucket");

            await store.AddOrUpdateAsync(metadata).ConfigureAwait(false);

            var collection = _databaseFixture.Database.GetCollection<StorageMetadataWrapper>(nameof(StorageMetadataWrapper));
            var actual = await collection.Find(p => p.Identity == metadata.Id).FirstOrDefaultAsync().ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(metadata.CorrelationId, actual!.CorrelationId);
            Assert.Equal(metadata.IsUploaded, actual!.IsUploaded);
            Assert.Equal(metadata.GetType().AssemblyQualifiedName, actual!.TypeName);
            Assert.Equal(JsonSerializer.Serialize<object>(metadata), actual!.Value);

            var unwrapped = actual.GetObject();
            Assert.NotNull(unwrapped);

            Assert.Equal(metadata.Workflows, unwrapped!.Workflows);
            Assert.Equal(metadata.File.TemporaryBucketName, unwrapped!.File.TemporaryBucketName);
        }

        [Fact]
        public async Task GivenACorrelationId_WhenGetFileStorageMetdadataIsCalled_ExpectMatchingFileStorageMetadataToBeReturned()
        {
            var correlationId = Guid.NewGuid();
            var list = new List<FileStorageMetadata>{
                    CreateMetadataObject(correlationId),
                    CreateMetadataObject(correlationId),
                    CreateMetadataObject(),
                    new FhirFileStorageMetadata(
                        correlationId.ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        FhirStorageFormat.Json),
                    new FhirFileStorageMetadata(
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        FhirStorageFormat.Json),
            };

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);

            foreach (var item in list)
            {
                await store.AddOrUpdateAsync(item).ConfigureAwait(false);
            }

            var results = await store.GetFileStorageMetdadataAsync(correlationId.ToString()).ConfigureAwait(false);

            Assert.Equal(3, results.Count);

            Assert.Collection(results,
                (item) => item!.Id.Equals(list[0].Id),
                (item) => item!.Id.Equals(list[1].Id),
                (item) => item!.Id.Equals(list[2].Id));
        }

        [Fact]
        public async Task GivenACorrelationIdAndAnIdentity_WhenGetFileStorageMetadadataIsCalled_ExpectMatchingFileStorageMetadataToBeReturned()
        {
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            var expected = new DicomFileStorageMetadata(
                        correlationId,
                        identifier,
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString());

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
            await store.AddOrUpdateAsync(expected).ConfigureAwait(false);

            var match = await store.GetFileStorageMetdadataAsync(correlationId, identifier).ConfigureAwait(false);

            Assert.NotNull(match);
            Assert.Equal(expected.Id, match!.Id);
            Assert.Equal(expected.CorrelationId, match.CorrelationId);
        }

        [Fact]
        public async Task GivenACorrelationIdAndAnIdentity_WhenDeleteAsyncIsCalled_ExpectMatchingInstanceToBeDeleted()
        {
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            var expected = new DicomFileStorageMetadata(
                        correlationId,
                        identifier,
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString());

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
            await store.AddAsync(expected).ConfigureAwait(false);
            var result = await store.DeleteAsync(correlationId, identifier).ConfigureAwait(false);
            Assert.True(result);

            var collection = _databaseFixture.Database.GetCollection<StorageMetadataWrapper>(nameof(StorageMetadataWrapper));
            var stored = await collection.Find(p => p.Identity == identifier).FirstOrDefaultAsync().ConfigureAwait(false);

            Assert.Null(stored);
        }

        [Fact]
        public async Task GivenACorrelationIdAndAnIdentity_WhenDeleteAsyncIsCalledWithoutAMatch_ExpectNothingIsDeleted()
        {
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            var pending = CreateMetadataObject();

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
            await store.AddAsync(pending).ConfigureAwait(false);

            var result = await store.DeleteAsync(correlationId, identifier).ConfigureAwait(false);
            Assert.False(result);

            var collection = _databaseFixture.Database.GetCollection<StorageMetadataWrapper>(nameof(StorageMetadataWrapper));
            var stored = await collection.Find(p => p.Identity == pending.Id).FirstOrDefaultAsync().ConfigureAwait(false);

            Assert.NotNull(stored);
        }

        [Fact]
        public async Task GivenStorageMetadataObjects_WhenDeletingPendingUploadsObject_ExpectAllPendingObjectsToBeDeleted()
        {
            var collection = _databaseFixture.Database.GetCollection<StorageMetadataWrapper>(nameof(StorageMetadataWrapper));
            MongoDatabaseFixture.Clear(collection);

            var pending = CreateMetadataObject();
            var uploaded = CreateMetadataObject();
            uploaded.File.SetUploaded("bucket");
            uploaded.JsonFile.SetUploaded("bucket");

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
            await store.AddAsync(pending).ConfigureAwait(false);
            await store.AddAsync(uploaded).ConfigureAwait(false);

            await store.DeletePendingUploadsAsync().ConfigureAwait(false);

            var result = await collection.Find(Builders<StorageMetadataWrapper>.Filter.Empty).ToListAsync().ConfigureAwait(false);
            Assert.Single(result);
            Assert.Equal(uploaded.Id, result[0].Identity);
        }

        private static DicomFileStorageMetadata CreateMetadataObject() => CreateMetadataObject(Guid.NewGuid());

        private static DicomFileStorageMetadata CreateMetadataObject(Guid corrleationId) => new(
                        corrleationId.ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString());
    }
}
