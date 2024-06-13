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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories;
using Monai.Deploy.Messaging.Events;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test
{
    [Collection("SqliteDatabase")]
    public class StorageMetadataWrapperRepositoryTest
    {
        private readonly SqliteDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<StorageMetadataWrapperRepository>> _logger;
        private readonly IOptions<DatabaseOptions> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public StorageMetadataWrapperRepositoryTest(SqliteDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<StorageMetadataWrapperRepository>>();
            _options = Options.Create(new DatabaseOptions());

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => databaseFixture.DatabaseContext);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void GivenStorageMetadataWrapperRepositoryType_WhenInitialized_TheConstructorShallGuardAllParameters()
        {
            Assert.Throws<ArgumentNullException>(() => new StorageMetadataWrapperRepository(null!, null!, null!));
            Assert.Throws<ArgumentNullException>(() => new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, null!));

            _ = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options);
        }

        [Fact]
        public async Task GivenADicomStorageMetadataObject_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var metadata = CreateMetadataObject();

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(metadata).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            var actual = await _databaseFixture.DatabaseContext.Set<StorageMetadataWrapper>().FirstOrDefaultAsync(p => p.Identity.Equals(metadata.Id)).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

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

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            await store.AddOrUpdateAsync(metadata1).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            await Assert.ThrowsAsync<ArgumentException>(async () => await store.UpdateAsync(metadata2).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext)).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
        }

        [Fact]
        public async Task GivenAnExistingDicomStorageMetadataObject_WhenUpdated_ExpectItToBeSaved()
        {
            var metadata = CreateMetadataObject();

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(metadata).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            metadata.SetWorkflows("A", "B", "C");
            metadata.File.SetUploaded("bucket");

            await store.AddOrUpdateAsync(metadata).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            var actual = await _databaseFixture.DatabaseContext.Set<StorageMetadataWrapper>().FirstOrDefaultAsync(p => p.Identity.Equals(metadata.Id)).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

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
                        FhirStorageFormat.Json,
                        DataService.FHIR,
                        "origin"),
                    new FhirFileStorageMetadata(
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        FhirStorageFormat.Json,
                        DataService.FHIR,
                        "origin"),
            };

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            foreach (var item in list)
            {
                await store.AddOrUpdateAsync(item).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            }

            var results = await store.GetFileStorageMetdadataAsync(correlationId.ToString()).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

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
                        Guid.NewGuid().ToString(),
                        DataService.DIMSE,
                        "calling",
                        "called");

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddOrUpdateAsync(expected).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            var match = await store.GetFileStorageMetdadataAsync(correlationId, identifier).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

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
                        Guid.NewGuid().ToString(),
                        DataService.DIMSE,
                        "calling",
                        "called");

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(expected).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            var result = await store.DeleteAsync(correlationId, identifier).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.True(result);

            var stored = await _databaseFixture.DatabaseContext.Set<StorageMetadataWrapper>().FirstOrDefaultAsync(p => p.Identity == identifier).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Null(stored);
        }

        [Fact]
        public async Task GivenACorrelationIdAndAnIdentity_WhenDeleteAsyncIsCalledWithoutAMatch_ExpectNothingIsDeleted()
        {
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            var pending = CreateMetadataObject();

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(pending).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            var result = await store.DeleteAsync(correlationId, identifier).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.False(result);

            var stored = await _databaseFixture.DatabaseContext.Set<StorageMetadataWrapper>().FirstOrDefaultAsync(p => p.Identity == pending.Id).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(stored);
        }

        [Fact]
        public async Task GivenStorageMetadataObjects_WhenDeletingPendingUploadsObject_ExpectAllPendingObjectsToBeDeleted()
        {
            _databaseFixture.Clear<StorageMetadataWrapper>();

            var pending = CreateMetadataObject();
            var uploaded = CreateMetadataObject();
            uploaded.File.SetUploaded("bucket");
            uploaded.JsonFile.SetUploaded("bucket");

            var store = new StorageMetadataWrapperRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(pending).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            await store.AddAsync(uploaded).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            await store.DeletePendingUploadsAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            var result = await _databaseFixture.DatabaseContext.Set<StorageMetadataWrapper>().ToListAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Single(result);
            Assert.Equal(uploaded.Id, result[0].Identity);
        }

        private static DicomFileStorageMetadata CreateMetadataObject() => CreateMetadataObject(Guid.NewGuid());

        private static DicomFileStorageMetadata CreateMetadataObject(Guid corrleationId) => new(
                        corrleationId.ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        DataService.DicomWeb,
                        "callingAET",
                        "calledAET");
    }
}
