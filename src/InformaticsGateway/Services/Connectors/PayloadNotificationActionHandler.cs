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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.Messaging.API;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    internal interface IPayloadNotificationActionHandler
    {
        Task NotifyAsync(Payload payload, ActionBlock<Payload> notificationQueue, CancellationToken cancellationToken = default);
    }

    internal class PayloadNotificationActionHandler : IPayloadNotificationActionHandler, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PayloadNotificationActionHandler> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly IServiceScope _scope;
        private bool _disposedValue;

        public PayloadNotificationActionHandler(IServiceScopeFactory serviceScopeFactory,
                                                ILogger<PayloadNotificationActionHandler> logger,
                                                IOptions<InformaticsGatewayConfiguration> options)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _scope = _serviceScopeFactory.CreateScope();
        }

        public async Task NotifyAsync(Payload payload, ActionBlock<Payload> notificationQueue, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(payload, nameof(payload));
            Guard.Against.Null(notificationQueue, nameof(notificationQueue));

            if (payload.State != Payload.PayloadState.Notify)
            {
                throw new PayloadNotifyException(PayloadNotifyException.FailureReason.IncorrectState);
            }

            try
            {
                await NotifyPayloadReady(payload).ConfigureAwait(false);
                await DeletePayload(payload, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                payload.RetryCount++;
                var action = await UpdatePayloadState(payload, cancellationToken).ConfigureAwait(false);
                if (action == PayloadAction.Updated)
                {
                    _logger.FailedToPublishWorkflowRequest(payload.PayloadId, ex);
                    if (!await notificationQueue.Post(payload, _options.Value.Messaging.Retries.RetryDelays.ElementAt(payload.RetryCount - 1)).ConfigureAwait(false))
                    {
                        throw new PostPayloadException(Payload.PayloadState.Notify, payload);
                    }
                }
            }
        }

        private async Task DeletePayload(Payload payload, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(payload, nameof(payload));

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetService<IPayloadRepository>() ?? throw new ServiceNotFoundException(nameof(IPayloadRepository));
            await repository.RemoveAsync(payload, cancellationToken).ConfigureAwait(false);
        }

        private async Task NotifyPayloadReady(Payload payload)
        {
            Guard.Against.Null(payload, nameof(payload));

            _logger.GenerateWorkflowRequest(payload.PayloadId);

            var workflowRequest = new WorkflowRequestEvent
            {
                Bucket = _options.Value.Storage.StorageServiceBucketName,
                PayloadId = payload.PayloadId,
                Workflows = payload.GetWorkflows(),
                FileCount = payload.Count,
                CorrelationId = payload.CorrelationId,
                Timestamp = payload.DateTimeCreated,
                DataTrigger = payload.DataTrigger,
                WorkflowInstanceId = payload.WorkflowInstanceId,
                TaskId = payload.TaskId,
            };
            workflowRequest.DataOrigins.AddRange(payload.DataOrigins);

            workflowRequest.AddFiles(payload.GetUploadedFiles().AsEnumerable());

            var message = new JsonMessage<WorkflowRequestEvent>(
                workflowRequest,
                MessageBrokerConfiguration.InformaticsGatewayApplicationId,
                payload.CorrelationId,
                string.Empty);

            _logger.PublishingWorkflowRequest(message.MessageId);

            var messageBrokerPublisherService = _scope.ServiceProvider.GetService<IMessageBrokerPublisherService>() ?? throw new ServiceNotFoundException(nameof(IMessageBrokerPublisherService));
            await messageBrokerPublisherService.Publish(
                _options.Value.Messaging.Topics.WorkflowRequest,
                message.ToMessage()).ConfigureAwait(false);

            _logger.WorkflowRequestPublished(_options.Value.Messaging.Topics.WorkflowRequest, message.MessageId, payload.Elapsed);
        }

        private async Task<PayloadAction> UpdatePayloadState(Payload payload, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(payload, nameof(payload));

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetService<IPayloadRepository>() ?? throw new ServiceNotFoundException(nameof(IPayloadRepository));

            try
            {
                if (payload.RetryCount > _options.Value.Storage.Retries.DelaysMilliseconds.Length)
                {
                    _logger.NotificationFailureStopRetry(payload.PayloadId);
                    await repository.RemoveAsync(payload, cancellationToken).ConfigureAwait(false);
                    _logger.PayloadDeleted(payload.PayloadId);
                    return PayloadAction.Deleted;
                }
                else
                {
                    _logger.NotificationFailureRetryLater(payload.PayloadId, payload.State, payload.RetryCount);
                    await repository.UpdateAsync(payload, cancellationToken).ConfigureAwait(false);
                    _logger.PayloadSaved(payload.PayloadId);
                    return PayloadAction.Updated;
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorUpdatingPayload(payload.PayloadId, ex);
                return PayloadAction.Updated;
            }
        }

        protected virtual void Dispose(bool disposing)
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