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

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test;
using Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories;
using Monai.Deploy.Messaging.Events;
using MongoDB.Driver;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Integration.Test
{
    [Collection("MongoDatabase")]
    public class PayloadRepositoryTest
    {
        private readonly MongoDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<PayloadRepository>> _logger;
        private readonly IOptions<DatabaseOptions> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public PayloadRepositoryTest(MongoDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<PayloadRepository>>();
            _options = _databaseFixture.Options;

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => databaseFixture.Client);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public async Task GivenAPayload_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var payload = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = DataService.DIMSE, Destination = "called", Source = "calling" }, 5);
            payload.Add(new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DataService.DIMSE, "calling", "called"));
            payload.Add(new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DataService.DIMSE, "calling1", "called1"));
            payload.Add(new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DataService.DIMSE, "calling2", "called2"));
            payload.State = Payload.PayloadState.Move;

            var store = new PayloadRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(payload).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            var collection = _databaseFixture.Database.GetCollection<Payload>(nameof(Payload));
            var actual = await collection.Find(p => p.PayloadId == payload.PayloadId).FirstOrDefaultAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            Assert.NotNull(actual);
            Assert.Equal(payload.Key, actual!.Key);
            Assert.Equal(payload.State, actual!.State);
            Assert.Equal(payload.Count, actual!.Count);
            Assert.Equal(payload.RetryCount, actual!.RetryCount);
            Assert.Equal(payload.CorrelationId, actual!.CorrelationId);
            Assert.Equal(payload.WorkflowInstanceId, actual!.WorkflowInstanceId);
            Assert.Equal(payload.TaskId, actual!.TaskId);
            Assert.Equal(payload.DataTrigger.Source, actual!.DataTrigger.Source);
            Assert.Equal(payload.DataTrigger.Destination, actual!.DataTrigger.Destination);
            Assert.Equal(payload.DataTrigger.DataService, actual!.DataTrigger.DataService);
            Assert.Equal(payload.Timeout, actual!.Timeout);
            actual!.Files.Should().BeEquivalentTo(payload.Files, options => options.Excluding(p => p.DateReceived));

            Assert.Equal(payload.DataTrigger, actual.Files[0].DataOrigin);
            Assert.Collection(payload.DataOrigins,
                item => item.Equals(actual.Files[1]),
                item => item.Equals(actual.Files[2]));
        }

        [Fact]
        public async Task GivenAPayload_WhenRemoveIsCalled_ExpectItToDeleted()
        {
            var payload = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5);
            payload.Add(new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DataService.DIMSE, "calling", "called"));
            payload.State = Payload.PayloadState.Move;

            var store = new PayloadRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            var added = await store.AddAsync(payload).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            var removed = await store.RemoveAsync(added!).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Same(removed, added);

            var collection = _databaseFixture.Database.GetCollection<Payload>(nameof(Payload));
            var dbResult = await collection.Find(p => p.PayloadId == payload.PayloadId).FirstOrDefaultAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Null(dbResult);
        }

        [Fact]
        public async Task GivenDestinationApplicationEntitiesInTheDatabase_WhenToListIsCalled_ExpectAllEntitiesToBeReturned()
        {
            var store = new PayloadRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            var collection = _databaseFixture.Database.GetCollection<Payload>(nameof(Payload));
            var expected = await collection.Find(Builders<Payload>.Filter.Empty).ToListAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            var actual = await store.ToListAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            actual.Should().BeEquivalentTo(expected, options => options.Excluding(p => p.DateTimeCreated).Excluding(p => p.Elapsed).Excluding(p => p.HasTimedOut));
        }

        [Fact]
        public async Task GivenAPayload_WhenUpdateIsCalled_ExpectItToSaved()
        {
            var payload = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = DataService.DIMSE, Destination = "dest", Source = "source" }, 5);
            payload.Add(new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DataService.DIMSE, "source", "dest"));

            var store = new PayloadRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            var added = await store.AddAsync(payload).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            added.State = Payload.PayloadState.Notify;
            added.Add(new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DataService.ACR, "calling", "called"));
            var updated = await store.UpdateAsync(payload).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.NotNull(updated);

            var collection = _databaseFixture.Database.GetCollection<Payload>(nameof(Payload));
            var actual = await collection.Find(p => p.PayloadId == payload.PayloadId).FirstOrDefaultAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            Assert.NotNull(actual);
            Assert.Equal(updated.Key, actual!.Key);
            Assert.Equal(updated.State, actual!.State);
            Assert.Equal(updated.Count, actual!.Count);
            Assert.Equal(updated.RetryCount, actual!.RetryCount);
            Assert.Equal(updated.CorrelationId, actual!.CorrelationId);
            Assert.Equal(updated.WorkflowInstanceId, actual!.WorkflowInstanceId);
            Assert.Equal(updated.TaskId, actual!.TaskId);
            Assert.Equal(updated.DataTrigger.Source, actual!.DataTrigger.Source);
            Assert.Equal(updated.DataTrigger.Destination, actual!.DataTrigger.Destination);
            Assert.Equal(updated.DataTrigger.DataService, actual!.DataTrigger.DataService);
            Assert.Equal(updated.Timeout, actual!.Timeout);
            actual!.Files.Should().BeEquivalentTo(payload.Files, options => options.Excluding(p => p.DateReceived));

            Assert.Equal(updated.DataTrigger, actual.Files[0].DataOrigin);

            Assert.Collection(updated.DataOrigins,
                item => item.Equals(actual.Files[1].DataOrigin));
        }

        [Fact]
        public async Task GivenPayloadsInDifferentStates_WhenRemovePendingPayloadsAsyncIsCalled_ExpectPendingPayloadsToBeRemoved()
        {
            var collection = _databaseFixture.Database.GetCollection<Payload>(nameof(Payload));
            MongoDatabaseFixture.Clear(collection);

            var payload1 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Created };
            var payload2 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Created };
            var payload3 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Move };
            var payload4 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Notify };
            var payload5 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Notify };

            var store = new PayloadRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = await store.AddAsync(payload1).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            _ = await store.AddAsync(payload2).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            _ = await store.AddAsync(payload3).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            _ = await store.AddAsync(payload4).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            _ = await store.AddAsync(payload5).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            var result = await store.RemovePendingPayloadsAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Equal(2, result);

            var actual = await collection.Find(Builders<Payload>.Filter.Empty).ToListAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Equal(3, actual.Count);

            foreach (var payload in actual)
            {
                Assert.NotEqual(Payload.PayloadState.Created, payload.State);
            }
        }

        [Fact]
        public async Task GivenPayloadsInDifferentStates_WhenGetPayloadsInStateAsyncIsCalled_ExpectMatchingPayloadsToBeReturned()
        {
            var collection = _databaseFixture.Database.GetCollection<Payload>(nameof(Payload));
            MongoDatabaseFixture.Clear(collection);

            var payload1 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Created };
            var payload2 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Created };
            var payload3 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Move };
            var payload4 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Notify };
            var payload5 = new Payload(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 5) { State = Payload.PayloadState.Notify };

            var store = new PayloadRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = await store.AddAsync(payload1).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            _ = await store.AddAsync(payload2).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            _ = await store.AddAsync(payload3).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            _ = await store.AddAsync(payload4).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            _ = await store.AddAsync(payload5).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            var result = await store.GetPayloadsInStateAsync(CancellationToken.None, Payload.PayloadState.Move).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Single(result);

            result = await store.GetPayloadsInStateAsync(CancellationToken.None, Payload.PayloadState.Created).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Equal(2, result.Count);

            result = await store.GetPayloadsInStateAsync(CancellationToken.None, Payload.PayloadState.Notify).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Equal(2, result.Count);

            result = await store.GetPayloadsInStateAsync(CancellationToken.None, Payload.PayloadState.Notify, Payload.PayloadState.Created).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Equal(4, result.Count);
        }
    }
}