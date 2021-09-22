// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    public abstract class ExportServiceBase : IHostedService, IMonaiService
    {
        internal event EventHandler ReportActionStarted;

        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IStorageInfoProvider _storageInfoProvider;
        private readonly DataExportConfiguration _dataExportConfiguration;
        private System.Timers.Timer _workerTimer;

        protected abstract string Agent { get; }
        protected abstract int Concurrentcy { get; }
        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public abstract string ServiceName { get; }

        public ExportServiceBase(
            ILogger logger,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IServiceScopeFactory serviceScopeFactory,
            IStorageInfoProvider storageInfoProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _storageInfoProvider = storageInfoProvider ?? throw new ArgumentNullException(nameof(storageInfoProvider));
            _dataExportConfiguration = configuration.Value.Export;
        }

        /// <summary>
        /// Override the <c>ExportDataBlockCallback</c> method to customize export logic.
        /// Must update <c>State</c> to either <c>Succeeded</c> or <c>Failed</c>.
        /// </summary>
        /// <param name="outputJob"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<OutputJob> ExportDataBlockCallback(OutputJob outputJob, CancellationToken cancellationToken);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SetupPolling(cancellationToken);

            Status = ServiceStatus.Running;
            _logger.LogInformation("Export Task Watcher Hosted Service started.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _workerTimer?.Stop();
            _workerTimer = null;
            _logger.LogInformation("Export Task Watcher Hosted Service is stopping.");
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        private void SetupPolling(CancellationToken cancellationToken)
        {
            _workerTimer = new System.Timers.Timer(_dataExportConfiguration.PollFrequencyMs);
            _workerTimer.Elapsed += (sender, e) =>
            {
                WorkerTimerElapsed(cancellationToken);
            };
            _workerTimer.AutoReset = false;
            _workerTimer.Start();
        }

        private void WorkerTimerElapsed(CancellationToken cancellationToken)
        {
            try
            {
                if (!_storageInfoProvider.HasSpaceAvailableForExport)
                {
                    _logger.Log(LogLevel.Warning, $"Export service paused due to insufficient storage space.  Available storage space: {_storageInfoProvider.AvailableFreeSpace:D}.");
                    return;
                }

                var executionOptions = new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Concurrentcy,
                    MaxMessagesPerTask = 1,
                    CancellationToken = cancellationToken
                };

                var downloadActionBlock = new TransformManyBlock<string, TaskResponse>(
                    async (agent) => await DownloadActionCallback(agent, cancellationToken),
                    executionOptions);

                var downloadPayloadTransformBlock = new TransformBlock<TaskResponse, OutputJob>(
                    async (task) => await DownloadPayloadBlockCallback(task, cancellationToken),
                    executionOptions);

                var exportActionBlock = new TransformBlock<OutputJob, OutputJob>(
                    async (task) => await ExportDataBlockCallback(task, cancellationToken),
                    executionOptions);

                var reportingActionBlock = new ActionBlock<OutputJob>(
                    async (task) => await ReportingActionBlock(task, cancellationToken),
                    executionOptions);

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                downloadActionBlock.LinkTo(downloadPayloadTransformBlock, linkOptions);
                downloadPayloadTransformBlock.LinkTo(exportActionBlock, linkOptions);
                exportActionBlock.LinkTo(reportingActionBlock, linkOptions);

                downloadActionBlock.Post(Agent);
                downloadActionBlock.Complete();
                reportingActionBlock.Completion.Wait();
                _logger.Log(LogLevel.Trace, "Export Service completed timer routine.");
            }
            catch (AggregateException ex)
            {
                foreach (var iex in ex.InnerExceptions)
                {
                    _logger.Log(LogLevel.Error, iex, "Error occurred while exporting.");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error processing export task.");
            }
            finally
            {
                _workerTimer?.Start();
            }
        }

        private async Task<IEnumerable<TaskResponse>> DownloadActionCallback(string agent, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var resultsService = scope.ServiceProvider.GetRequiredService<IWorkloadManagerApi>();
            return await resultsService.GetPendingJobs(agent, 10, cancellationToken);
        }

        private async Task<OutputJob> DownloadPayloadBlockCallback(TaskResponse task, CancellationToken cancellationToken)
        {
            Guard.Against.Null(task, nameof(task));
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", task.ExportTaskId }, { "CorrelationId", task.CorrelationId } });
            var scope = _serviceScopeFactory.CreateScope();
            var workloadManager = scope.ServiceProvider.GetRequiredService<IWorkloadManagerApi>();

            try
            {
                var file = await workloadManager.Download(task.ApplicationId, task.FileId, cancellationToken);
                return new OutputJob(task, file);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Failed to download file.");
                await ReportFailure(task, cancellationToken);
                return null;
            }
        }

        private async Task ReportingActionBlock(OutputJob job, CancellationToken cancellationToken)
        {
            if (ReportActionStarted != null)
            {
                ReportActionStarted(this, null);
            }

            if (job is null)
            {
                return;
            }

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", job.ExportTaskId }, { "CorrelationId", job.CorrelationId } });
            await ReportStatus(job, cancellationToken);
        }

        protected async Task ReportStatus(OutputJob job, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", job.ExportTaskId }, { "CorrelationId", job.CorrelationId } });

            if (job is null)
            {
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var workloadManager = scope.ServiceProvider.GetRequiredService<IWorkloadManagerApi>();

            try
            {
                if (job.State == State.Succeeded)
                {
                    await workloadManager.ReportSuccess(job.ExportTaskId, cancellationToken);
                    _logger.Log(LogLevel.Information, "Task marked as successful.");
                }
                else if (job.State == State.Failed)
                {
                    await workloadManager.ReportFailure(job.ExportTaskId, job.Retries > _dataExportConfiguration.MaximumRetries, cancellationToken);
                    _logger.Log(LogLevel.Warning, "Task marked as failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Failed to report status back to Results Service.");
            }
        }

        protected async Task ReportFailure(TaskResponse job, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", job.ExportTaskId }, { "CorrelationId", job.CorrelationId } });
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var workloadManager = scope.ServiceProvider.GetRequiredService<IWorkloadManagerApi>();
                await workloadManager.ReportFailure(job.ExportTaskId, false, cancellationToken);
                _logger.Log(LogLevel.Warning, $"Task {job.ExportTaskId} marked as failure and will not be retried.");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex, "Failed to mark task {0} as failure.", job.ExportTaskId);
            }
        }
    }
}
