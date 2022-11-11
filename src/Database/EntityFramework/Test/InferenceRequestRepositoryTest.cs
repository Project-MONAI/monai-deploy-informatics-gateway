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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test
{
    [Collection("SqliteDatabase")]
    public class InferenceRequestRepositoryTest
    {
        private readonly SqliteDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<InferenceRequestRepository>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public InferenceRequestRepositoryTest(SqliteDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;// new SqliteDatabaseFixture();

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<InferenceRequestRepository>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => _databaseFixture.DatabaseContext);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Database.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public async Task GivenAnInferenceRequest_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var inferenceRequest = CreateInferenceRequest();

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(inferenceRequest).ConfigureAwait(false);
            var actual = await _databaseFixture.DatabaseContext.Set<InferenceRequest>().FirstOrDefaultAsync(p => p.InferenceRequestId.Equals(inferenceRequest.InferenceRequestId)).ConfigureAwait(false);

            Assert.NotNull(actual);
            Assert.Equal(inferenceRequest.InferenceRequestId, actual!.InferenceRequestId);
            Assert.Equal(inferenceRequest.State, actual!.State);
            Assert.Equal(inferenceRequest.Status, actual!.Status);
            Assert.Equal(inferenceRequest.TransactionId, actual!.TransactionId);
            Assert.Equal(inferenceRequest.TryCount, actual!.TryCount);
        }

        [Fact]
        public async Task GivenAFailedInferenceRequstThatExceededRetries_WhenUpdateIsCalled_ShallMarkAsFailed()
        {
            var inferenceRequest = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
                TryCount = 3
            };

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(inferenceRequest).ConfigureAwait(false);
            await store.UpdateAsync(inferenceRequest, InferenceRequestStatus.Fail);

            var result = await _databaseFixture.DatabaseContext.Set<InferenceRequest>().FirstOrDefaultAsync(p => p.TransactionId == inferenceRequest.TransactionId).ConfigureAwait(false);
            Assert.NotNull(result);
            Assert.Equal(InferenceRequestState.Completed, result!.State);
            Assert.Equal(InferenceRequestStatus.Fail, result!.Status);
        }

        [Fact]
        public async Task GivenAFailedInferenceRequst_WhenUpdateIsCalled_ShallRetryLater()
        {
            var inferenceRequest = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
                TryCount = 1
            };

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(inferenceRequest).ConfigureAwait(false);
            await store.UpdateAsync(inferenceRequest, InferenceRequestStatus.Fail).ConfigureAwait(false);

            var result = await _databaseFixture.DatabaseContext.Set<InferenceRequest>().FirstOrDefaultAsync(p => p.TransactionId == inferenceRequest.TransactionId).ConfigureAwait(false);
            Assert.NotNull(result);
            Assert.Equal(InferenceRequestState.Queued, result!.State);
            Assert.Equal(InferenceRequestStatus.Unknown, result!.Status);
            Assert.Equal(2, result!.TryCount);
        }

        [Fact]
        public async Task GivenASuccessfulInferenceRequest_WhenUpdateIsCalled_ShallMarkAsCompleted()
        {
            var inferenceRequest = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString()
            };

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);

            await store.AddAsync(inferenceRequest).ConfigureAwait(false);
            await store.UpdateAsync(inferenceRequest, InferenceRequestStatus.Success).ConfigureAwait(false);

            var result = await _databaseFixture.DatabaseContext.Set<InferenceRequest>().FirstOrDefaultAsync(p => p.TransactionId == inferenceRequest.TransactionId).ConfigureAwait(false);
            Assert.NotNull(result);
            Assert.Equal(InferenceRequestState.Completed, result!.State);
            Assert.Equal(InferenceRequestStatus.Success, result!.Status);
        }

        [Fact]
        public async Task GivenAQueuedInferenceRequests_WhenTakeIsCalled_ShallReturnFirstQueued()
        {
            var set = _databaseFixture.DatabaseContext.Set<InferenceRequest>();
            set.RemoveRange(set.ToList());
            await _databaseFixture.DatabaseContext.SaveChangesAsync().ConfigureAwait(false);
            var inferenceRequestInProcess = CreateInferenceRequest(InferenceRequestState.InProcess);
            var inferenceRequestCompleted = CreateInferenceRequest(InferenceRequestState.Completed);
            var inferenceRequestQueued = CreateInferenceRequest();

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(inferenceRequestInProcess).ConfigureAwait(false);
            await store.AddAsync(inferenceRequestCompleted).ConfigureAwait(false);
            await store.AddAsync(inferenceRequestQueued).ConfigureAwait(false);

            var actual = await store.TakeAsync().ConfigureAwait(false);
            Assert.NotNull(actual);
            Assert.Equal(inferenceRequestQueued.InferenceRequestId, actual!.InferenceRequestId);
            Assert.Equal(InferenceRequestState.InProcess, actual!.State);
            Assert.Equal(inferenceRequestQueued.Status, actual!.Status);
            Assert.Equal(inferenceRequestQueued.TransactionId, actual!.TransactionId);
            Assert.Equal(inferenceRequestQueued.TryCount, actual!.TryCount);
        }

        [Fact]
        public async Task GivenNoQueuedInferenceRequests_WhenTakeIsCalled_ShallReturnNotReturnAnything()
        {
            _databaseFixture.Clear<InferenceRequest>();

            var cancellationTokenSource = new CancellationTokenSource();
            var inferenceRequestInProcess = CreateInferenceRequest(InferenceRequestState.InProcess);
            var inferenceRequestCompleted = CreateInferenceRequest(InferenceRequestState.Completed);

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(inferenceRequestInProcess).ConfigureAwait(false);
            await store.AddAsync(inferenceRequestCompleted).ConfigureAwait(false);

            cancellationTokenSource.CancelAfter(500);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await store.TakeAsync(cancellationTokenSource.Token).ConfigureAwait(false));
        }

        [Fact]
        public async Task GivenInferenceRequests_WhenGetInferenceRequestIsCalled_ShallReturnMatchingObject()
        {
            var inferenceRequest1 = CreateInferenceRequest();
            var inferenceRequest2 = CreateInferenceRequest();
            var inferenceRequest3 = CreateInferenceRequest();

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(inferenceRequest1).ConfigureAwait(false);
            await store.AddAsync(inferenceRequest2).ConfigureAwait(false);
            await store.AddAsync(inferenceRequest3).ConfigureAwait(false);

            var result = await store.GetInferenceRequestAsync(inferenceRequest1.TransactionId).ConfigureAwait(false);
            Assert.Equal(inferenceRequest1.TransactionId, result.TransactionId);
            result = await store.GetInferenceRequestAsync(inferenceRequest2.TransactionId).ConfigureAwait(false);
            Assert.Equal(inferenceRequest2.TransactionId, result.TransactionId);
            result = await store.GetInferenceRequestAsync(inferenceRequest3.TransactionId).ConfigureAwait(false);
            Assert.Equal(inferenceRequest3.TransactionId, result.TransactionId);

            result = await store.GetInferenceRequestAsync(inferenceRequest1.InferenceRequestId).ConfigureAwait(false);
            Assert.Equal(inferenceRequest1.TransactionId, result.TransactionId);
            result = await store.GetInferenceRequestAsync(inferenceRequest2.InferenceRequestId).ConfigureAwait(false);
            Assert.Equal(inferenceRequest2.TransactionId, result.TransactionId);
            result = await store.GetInferenceRequestAsync(inferenceRequest3.InferenceRequestId).ConfigureAwait(false);
            Assert.Equal(inferenceRequest3.TransactionId, result.TransactionId);
        }

        [Fact]
        public async Task GivenInferenceRequests_WhenExistsCalled_ShallReturnCorrectValue()
        {
            var inferenceRequest = CreateInferenceRequest();

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(inferenceRequest).ConfigureAwait(false);

            var result = await store.ExistsAsync(inferenceRequest.TransactionId).ConfigureAwait(false);
            Assert.True(result);

            result = await store.ExistsAsync("random").ConfigureAwait(false);
            Assert.False(result);
        }

        [Fact]
        public async Task GivenAMatchingInferenceRequest_WhenGetStatusCalled_ShallReturnStatus()
        {
            var inferenceRequest = CreateInferenceRequest();

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(inferenceRequest).ConfigureAwait(false);

            var result = await store.GetStatusAsync(inferenceRequest.TransactionId).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(inferenceRequest.TransactionId, result.TransactionId);
        }

        [Fact]
        public async Task GivenNoMatchingInferenceRequest_WhenGetStatusCalled_ShallReturnStatus()
        {
            var inferenceRequest = CreateInferenceRequest();

            var store = new InferenceRequestRepository(_serviceScopeFactory.Object, _logger.Object, _options);
            await store.AddAsync(inferenceRequest).ConfigureAwait(false);

            var result = await store.GetStatusAsync("bogus").ConfigureAwait(false);

            Assert.Null(result);
        }

        private InferenceRequest CreateInferenceRequest(InferenceRequestState state = InferenceRequestState.Queued) => new InferenceRequest
        {
            InferenceRequestId = Guid.NewGuid(),
            State = state,
            Status = InferenceRequestStatus.Unknown,
            TransactionId = Guid.NewGuid().ToString(),
            TryCount = 0,
        };
    }
}
