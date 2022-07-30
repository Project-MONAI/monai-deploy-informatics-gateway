/*
 * Copyright 2022 MONAI Consortium
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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database;
using Monai.Deploy.InformaticsGateway.Repositories;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Repositories
{
    public class StorageMetadataWrapperRepositoryTest
    {
        private readonly Mock<ILogger<StorageMetadataWrapperRepository>> _logger;
        private readonly Mock<IInformaticsGatewayRepository<StorageMetadataWrapper>> _repository;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        public StorageMetadataWrapperRepositoryTest()
        {
            _logger = new Mock<ILogger<StorageMetadataWrapperRepository>>();
            _repository = new Mock<IInformaticsGatewayRepository<StorageMetadataWrapper>>();
            _options = Options.Create<InformaticsGatewayConfiguration>(new InformaticsGatewayConfiguration());

            _options.Value.Database.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void GivenStorageMetadataWrapperRepositoryType_WhenInitialized_TheConstructorShallGuardAllParameters()
        {
            Assert.Throws<ArgumentNullException>(() => new StorageMetadataWrapperRepository(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, null));
            Assert.Throws<ArgumentNullException>(() => new StorageMetadataWrapperRepository(_logger.Object, null, null));

            _ = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
        }

        [Fact]
        public async Task GivenADicomStorageMetadataObject_WhenSavingToDatabaseWithException_ExpectAddAsyncToAttemptRetries()
        {
            _repository.Setup(p => p.AddAsync(It.IsAny<StorageMetadataWrapper>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);

            await Assert.ThrowsAsync<Exception>(async () => await store.AddAsync(metadata));

            _repository.Verify(p => p.AddAsync(It.IsAny<StorageMetadataWrapper>(), It.IsAny<CancellationToken>()), Times.AtLeast(3));
        }

        [Fact]
        public async Task GivenADicomStorageMetadataObject_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());
            var json = JsonSerializer.Serialize(metadata);

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
            await store.AddAsync(metadata);

            _repository.Verify(p => p.AddAsync(It.IsAny<StorageMetadataWrapper>(), It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GivenADicomStorageMetadataObject_WhenSavingToDatabaseWithException_ExpectUpdateAsyncToAttemptRetries()
        {
            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<StorageMetadataWrapper, bool>>()))
                .Returns(new StorageMetadataWrapper(metadata));

            _repository.Setup(p => p.Update(It.IsAny<StorageMetadataWrapper>()))
                .Throws(new Exception("error"));

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
            await store.AddAsync(metadata);
            metadata.SetWorkflows("A", "B", "C");
            metadata.File.SetUploaded("bucket");

            await Assert.ThrowsAsync<Exception>(async () => await store.UpdateAsync(metadata));

            _repository.Verify(p => p.Update(It.IsAny<StorageMetadataWrapper>()), Times.AtLeast(3));
        }

        [Fact]
        public async Task GivenANonExistedDicomStorageMetadataObject_WhenSavedToDatabase_ThrowsArgumentException()
        {
            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<StorageMetadataWrapper, bool>>()))
                .Returns(default(StorageMetadataWrapper));

            _repository.Setup(p => p.Update(It.IsAny<StorageMetadataWrapper>()));

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
            await store.AddOrUpdateAsync(metadata);
            metadata.SetWorkflows("A", "B", "C");
            metadata.File.SetUploaded("bucket");

            await Assert.ThrowsAsync<ArgumentException>(async () => await store.UpdateAsync(metadata));

            _repository.Verify(p => p.FirstOrDefault(It.IsAny<Func<StorageMetadataWrapper, bool>>()), Times.AtLeast(3));
            _repository.Verify(p => p.Update(It.IsAny<StorageMetadataWrapper>()), Times.Never());
        }

        [Fact]
        public async Task GivenAnExistingDicomStorageMetadataObject_WhenUpdated_ExpectItToBeSaved()
        {
            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<StorageMetadataWrapper, bool>>()))
                .Returns(new StorageMetadataWrapper(metadata));

            _repository.Setup(p => p.Update(It.IsAny<StorageMetadataWrapper>()));

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
            await store.AddAsync(metadata);
            metadata.SetWorkflows("A", "B", "C");
            metadata.File.SetUploaded("bucket");

            await store.AddOrUpdateAsync(metadata);

            _repository.Verify(p => p.Update(It.IsAny<StorageMetadataWrapper>()), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GivenStorageMetadataObjects_WhenDeletingPendingUploadsObject_ExpectAllPendingObjectsToBeDeleted()
        {
            var pending = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());
            var uploaded = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());
            uploaded.File.SetUploaded("bucket");
            uploaded.JsonFile.SetUploaded("bucket");

            var files = new List<StorageMetadataWrapper> { new StorageMetadataWrapper(pending), new StorageMetadataWrapper(uploaded) };

            _repository.Setup(p => p.AsQueryable()).Returns(files.AsQueryable());

            _repository.Setup(p => p.Update(It.IsAny<StorageMetadataWrapper>()));

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
            await store.DeletePendingUploadsAsync();

            _repository.Verify(p => p.RemoveRange(It.Is<StorageMetadataWrapper[]>(p => p.Count() == 1)), Times.Once());
            _repository.Verify(p => p.RemoveRange(It.Is<StorageMetadataWrapper[]>(p => p.First().Identity == pending.Id)), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public void GivenACorrelationId_WhenGetFileStorageMetdadataIsCalled_ExpectMatchingFileStorageMetadataToBeReturned()
        {
            var correlationId = Guid.NewGuid().ToString();
            var list = new List<StorageMetadataWrapper>{
                new StorageMetadataWrapper(
                    new DicomFileStorageMetadata(
                        correlationId,
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString())),
                new StorageMetadataWrapper(
                    new DicomFileStorageMetadata(
                        correlationId,
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString())),
                new StorageMetadataWrapper(
                    new DicomFileStorageMetadata(
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString())),
                new StorageMetadataWrapper(
                    new FhirFileStorageMetadata(
                        correlationId,
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Api.Rest.FhirStorageFormat.Json)),
                new StorageMetadataWrapper(
                    new FhirFileStorageMetadata(
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Api.Rest.FhirStorageFormat.Json)),
            };
            _repository.Setup(p => p.AsQueryable())
                .Returns(list.AsQueryable());

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
            var results = store.GetFileStorageMetdadata(correlationId);

            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void GivenACorrelationIdAndAnIdentity_WhenGetFileStorageMetdadataIsCalled_ExpectMatchingFileStorageMetadataToBeReturned()
        {
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            var expected = new DicomFileStorageMetadata(
                        correlationId,
                        identifier,
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString());

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<StorageMetadataWrapper, bool>>()))
                .Returns(new StorageMetadataWrapper(expected));

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
            var match = store.GetFileStorageMetdadata(correlationId, identifier);

            Assert.Equal(expected.Id, match.Id);
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

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<StorageMetadataWrapper, bool>>()))
                .Returns(new StorageMetadataWrapper(expected));

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
            var result = await store.DeleteAsync(correlationId, identifier);

            Assert.True(result);

            _repository.Verify(p => p.Remove(It.IsAny<StorageMetadataWrapper>()), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GivenACorrelationIdAndAnIdentity_WhenDeleteAsyncIsCalledWithoutAMatch_ExpectNothingIsDeleted()
        {
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<StorageMetadataWrapper, bool>>()))
                .Returns(default(StorageMetadataWrapper));

            var store = new StorageMetadataWrapperRepository(_logger.Object, _repository.Object, _options);
            var result = await store.DeleteAsync(correlationId, identifier);

            Assert.False(result);

            _repository.Verify(p => p.Remove(It.IsAny<StorageMetadataWrapper>()), Times.Never());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
        }
    }
}
