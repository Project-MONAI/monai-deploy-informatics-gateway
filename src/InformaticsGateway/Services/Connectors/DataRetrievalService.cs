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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    internal class DataRetrievalService : IHostedService, IMonaiService, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly ILogger<DataRetrievalService> _logger;
        private readonly IServiceScope _rootScope;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IObjectUploadQueue _uploadQueue;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IDicomToolkit _dicomToolkit;
        private bool _disposedValue;

        public ServiceStatus Status { get; set; }

        public string ServiceName => "Data Retrieval Service";

        public DataRetrievalService(
            ILogger<DataRetrievalService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<InformaticsGatewayConfiguration> options)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _rootScope = _serviceScopeFactory.CreateScope();

            _httpClientFactory = _rootScope.ServiceProvider.GetService<IHttpClientFactory>() ?? throw new ServiceNotFoundException(nameof(IHttpClientFactory));
            _loggerFactory = _rootScope.ServiceProvider.GetService<ILoggerFactory>() ?? throw new ServiceNotFoundException(nameof(ILoggerFactory));
            _uploadQueue = _rootScope.ServiceProvider.GetService<IObjectUploadQueue>() ?? throw new ServiceNotFoundException(nameof(IObjectUploadQueue));
            _payloadAssembler = _rootScope.ServiceProvider.GetService<IPayloadAssembler>() ?? throw new ServiceNotFoundException(nameof(IPayloadAssembler));
            _dicomToolkit = _rootScope.ServiceProvider.GetService<IDicomToolkit>() ?? throw new ServiceNotFoundException(nameof(IDicomToolkit));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(async () =>
            {
                await BackgroundProcessing(cancellationToken).ConfigureAwait(true);
            }, CancellationToken.None);

            Status = ServiceStatus.Running;
            _logger.ServiceRunning(ServiceName);
            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        private async Task BackgroundProcessing(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInferenceRequestRepository>();

                InferenceRequest request = null;
                try
                {
                    request = await repository.Take(cancellationToken).ConfigureAwait(false);
                    using (_logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", request.TransactionId } }))
                    {
                        _logger.ProcessingInferenceRequest();
                        await ProcessRequest(request, cancellationToken).ConfigureAwait(false);
                        await repository.Update(request, InferenceRequestStatus.Success).ConfigureAwait(false);
                        _logger.InferenceRequestProcessed();
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.ServiceCancelledWithException(ServiceName, ex);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.ServiceDisposed(ServiceName, ex);
                }
                catch (Exception ex)
                {
                    _logger.ErrorProcessingInferenceRequest(request?.TransactionId, ex);
                    if (request != null)
                    {
                        await repository.Update(request, InferenceRequestStatus.Fail).ConfigureAwait(false);
                    }
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.ServiceCancelled(ServiceName);
        }

        private async Task ProcessRequest(InferenceRequest inferenceRequest, CancellationToken cancellationToken)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            var retrievedFiles = new Dictionary<string, FileStorageMetadata>(StringComparer.OrdinalIgnoreCase);
            RestoreExistingInstances(inferenceRequest, retrievedFiles, cancellationToken);

            foreach (var source in inferenceRequest.InputResources)
            {
                _logger.ProcessingInputResource(source.Interface, source.ConnectionDetails.Uri);
                switch (source.Interface)
                {
                    case InputInterfaceType.DicomWeb:
                        await RetrieveViaDicomWeb(inferenceRequest, source, retrievedFiles, cancellationToken).ConfigureAwait(false);
                        break;

                    case InputInterfaceType.Fhir:
                        await RetrieveViaFhir(inferenceRequest, source, retrievedFiles, cancellationToken).ConfigureAwait(false);
                        break;

                    case InputInterfaceType.Algorithm:
                        continue;
                    default:
                        _logger.UnsupportedInputInterface(source.Interface);
                        break;
                }
            }

            await NotifyNewInstance(inferenceRequest, retrievedFiles);
        }

        private async Task NotifyNewInstance(InferenceRequest inferenceRequest, Dictionary<string, FileStorageMetadata> retrievedFiles)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            if (retrievedFiles.IsNullOrEmpty())
            {
                throw new InferenceRequestException("No files found/retrieved with the request.");
            }

            foreach (var key in retrievedFiles.Keys)
            {
                if (inferenceRequest.Application is not null)
                {
                    retrievedFiles[key].SetWorkflows(inferenceRequest.Application.Id);
                }
                _uploadQueue.Queue(retrievedFiles[key]);
                await _payloadAssembler.Queue(inferenceRequest.TransactionId, retrievedFiles[key]).ConfigureAwait(false);
            }
        }

        private void RestoreExistingInstances(InferenceRequest inferenceRequest, Dictionary<string, FileStorageMetadata> retrievedInstances, CancellationToken cancellationToken)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));
            Guard.Against.Null(retrievedInstances, nameof(retrievedInstances));

            using var scope = _serviceScopeFactory.CreateScope();

            _logger.RestoringRetrievedFiles();
            var repository = _rootScope.ServiceProvider.GetService<IStorageMetadataWrapperRepository>() ?? throw new ServiceNotFoundException(nameof(IStorageMetadataWrapperRepository));
            var files = repository.GetFileStorageMetdadata(inferenceRequest.TransactionId);

            foreach (var file in files)
            {
                if (file is DicomFileStorageMetadata dicomFileInfo)
                {
                    retrievedInstances.Add(dicomFileInfo.Id, dicomFileInfo);
                }
                else if (file is FhirFileStorageMetadata fhirFileInfo)
                {
                    retrievedInstances.Add(fhirFileInfo.Id, fhirFileInfo);
                }
                _logger.RestoredFile(file.Id);
            }
        }

        #region Data Retrieval

        private async Task RetrieveViaFhir(InferenceRequest inferenceRequest, RequestInputDataResource source, Dictionary<string, FileStorageMetadata> retrievedResources, CancellationToken cancellationToken)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));
            Guard.Against.Null(retrievedResources, nameof(retrievedResources));

            foreach (var input in inferenceRequest.InputMetadata.Inputs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (input.Resources.IsNullOrEmpty())
                {
                    continue;
                }
                await RetrieveFhirResources(inferenceRequest.TransactionId, input, source, retrievedResources, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RetrieveFhirResources(string transactionId, InferenceRequestDetails requestDetails, RequestInputDataResource source, Dictionary<string, FileStorageMetadata> retrievedResources, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(requestDetails, nameof(requestDetails));
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(retrievedResources, nameof(retrievedResources));

            var pendingResources = new Queue<FhirResource>(requestDetails.Resources.Where(p => !p.IsRetrieved));

            if (pendingResources.Count == 0)
            {
                return;
            }

            var authenticationHeaderValue = AuthenticationHeaderValueExtensions.ConvertFrom(source.ConnectionDetails.AuthType, source.ConnectionDetails.AuthId);

            var httpClient = _httpClientFactory.CreateClient("fhir");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = authenticationHeaderValue;

            FhirResource resource = null;
            try
            {
                while (pendingResources.Count > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    resource = pendingResources.Dequeue();
                    resource.IsRetrieved = await RetrieveFhirResource(
                        transactionId,
                        httpClient,
                        resource,
                        source,
                        retrievedResources,
                        requestDetails.FhirFormat,
                        requestDetails.FhirAcceptHeader,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorRetrievingFhirResource(resource?.Type, resource?.Id, ex);
                throw;
            }
        }

        private async Task<bool> RetrieveFhirResource(string transactionId, HttpClient httpClient, FhirResource resource, RequestInputDataResource source, Dictionary<string, FileStorageMetadata> retrievedResources, FhirStorageFormat fhirFormat, string acceptHeader, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(httpClient, nameof(httpClient));
            Guard.Against.Null(resource, nameof(resource));
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(retrievedResources, nameof(retrievedResources));
            Guard.Against.NullOrWhiteSpace(acceptHeader, nameof(acceptHeader));

            var id = $"{resource.Type}/{resource.Id}";
            if (retrievedResources.ContainsKey(id))
            {
                _logger.FhireResourceAlreadyExists(id);
                return true;
            }

            _logger.RetrievingFhirResource(resource.Type, resource.Id, acceptHeader, fhirFormat);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{source.ConnectionDetails.Uri}{resource.Type}/{resource.Id}");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
            var response = await Policy
                .HandleResult<HttpResponseMessage>(p => !p.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    _options.Value.Fhir.Retries.RetryDelays,
                    (result, timeSpan, retryCount, context) =>
                    {
                        _logger.ErrorRetrievingFhirResourceWithRetry(resource.Type, resource.Id, result.Result.StatusCode, retryCount, result.Exception);
                    })
                .ExecuteAsync(async () => await httpClient.SendAsync(request).ConfigureAwait(false)).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.FhirResourceContainsNoData(resource.Type, resource.Id);
                    return false;
                }

                var fhirFile = new FhirFileStorageMetadata(transactionId, resource.Type, resource.Id, fhirFormat);
                await fhirFile.SetDataStream(json).ConfigureAwait(false);
                retrievedResources.Add(fhirFile.Id, fhirFile);
                return true;
            }
            else
            {
                _logger.ErrorRetrievingFhirResourceWithStatus(resource.Type, resource.Id, response.StatusCode);
                return false;
            }
        }

        private async Task RetrieveViaDicomWeb(InferenceRequest inferenceRequest, RequestInputDataResource source, Dictionary<string, FileStorageMetadata> retrievedInstance, CancellationToken cancellationToken)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));
            Guard.Against.Null(retrievedInstance, nameof(retrievedInstance));

            var authenticationHeaderValue = AuthenticationHeaderValueExtensions.ConvertFrom(source.ConnectionDetails.AuthType, source.ConnectionDetails.AuthId);

            var dicomWebClient = new DicomWebClient(_httpClientFactory.CreateClient("dicomweb"), _loggerFactory.CreateLogger<DicomWebClient>());
            dicomWebClient.ConfigureServiceUris(new Uri(source.ConnectionDetails.Uri, UriKind.Absolute));

            if (authenticationHeaderValue is not null)
            {
                dicomWebClient.ConfigureAuthentication(authenticationHeaderValue);
            }

            foreach (var input in inferenceRequest.InputMetadata.Inputs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                switch (input.Type)
                {
                    case InferenceRequestType.DicomUid:
                        await RetrieveStudies(inferenceRequest.TransactionId, dicomWebClient, input.Studies, retrievedInstance, cancellationToken).ConfigureAwait(false);
                        break;

                    case InferenceRequestType.DicomPatientId:
                        await QueryStudies(inferenceRequest.TransactionId, dicomWebClient, inferenceRequest, retrievedInstance, $"{DicomTag.PatientID.Group:X4}{DicomTag.PatientID.Element:X4}", input.PatientId, cancellationToken).ConfigureAwait(false);
                        break;

                    case InferenceRequestType.AccessionNumber:
                        foreach (var accessionNumber in input.AccessionNumber)
                        {
                            await QueryStudies(inferenceRequest.TransactionId, dicomWebClient, inferenceRequest, retrievedInstance, $"{DicomTag.AccessionNumber.Group:X4}{DicomTag.AccessionNumber.Element:X4}", accessionNumber, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case InferenceRequestType.FhireResource:
                        continue;
                    default:
                        throw new InferenceRequestException($"The 'inputMetadata' type '{input.Type}' specified is not supported.");
                }
            }
        }

        private async Task QueryStudies(string transactionId, DicomWebClient dicomWebClient, InferenceRequest inferenceRequest, Dictionary<string, FileStorageMetadata> retrievedInstance, string dicomTag, string queryValue, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(dicomWebClient, nameof(dicomWebClient));
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));
            Guard.Against.Null(retrievedInstance, nameof(retrievedInstance));
            Guard.Against.NullOrWhiteSpace(dicomTag, nameof(dicomTag));
            Guard.Against.NullOrWhiteSpace(queryValue, nameof(queryValue));

            _logger.PerformQido(dicomTag, queryValue);
            var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { dicomTag, queryValue }
            };

            var studies = new List<RequestedStudy>();
            await foreach (var result in dicomWebClient.Qido.SearchForStudies<DicomDataset>(queryParams))
            {
                if (result.Contains(DicomTag.StudyInstanceUID))
                {
                    var studyInstanceUid = result.GetString(DicomTag.StudyInstanceUID);
                    studies.Add(new RequestedStudy
                    {
                        StudyInstanceUid = studyInstanceUid
                    });
                    _logger.StudyFoundWithQido(studyInstanceUid, dicomTag, queryValue);
                }
                else
                {
                    _logger.InstanceMissingStudyInstanceUid(result.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, "UKNOWN"));
                }
            }

            if (studies.Count != 0)
            {
                await RetrieveStudies(transactionId, dicomWebClient, studies, retrievedInstance, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.QidoCompletedWithNoResult(dicomTag, queryValue);
            }
        }

        private async Task RetrieveStudies(string transactionId, IDicomWebClient dicomWebClient, IList<RequestedStudy> studies, Dictionary<string, FileStorageMetadata> retrievedInstance, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(studies, nameof(studies));
            Guard.Against.Null(retrievedInstance, nameof(retrievedInstance));

            foreach (var study in studies)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (study.Series.IsNullOrEmpty())
                {
                    _logger.RetrievingStudyWithWado(study.StudyInstanceUid);
                    var files = dicomWebClient.Wado.Retrieve(study.StudyInstanceUid);
                    await SaveFiles(transactionId, files, retrievedInstance, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await RetrieveSeries(transactionId, dicomWebClient, study, retrievedInstance, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task RetrieveSeries(string transactionId, IDicomWebClient dicomWebClient, RequestedStudy study, Dictionary<string, FileStorageMetadata> retrievedInstance, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(study, nameof(study));
            Guard.Against.Null(retrievedInstance, nameof(retrievedInstance));

            foreach (var series in study.Series)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (series.Instances.IsNullOrEmpty())
                {
                    _logger.RetrievingSeriesWithWado(series.SeriesInstanceUid);
                    var files = dicomWebClient.Wado.Retrieve(study.StudyInstanceUid, series.SeriesInstanceUid);
                    await SaveFiles(transactionId, files, retrievedInstance, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await RetrieveInstances(transactionId, dicomWebClient, study.StudyInstanceUid, series, retrievedInstance, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task RetrieveInstances(string transactionId, IDicomWebClient dicomWebClient, string studyInstanceUid, RequestedSeries series, Dictionary<string, FileStorageMetadata> retrievedInstance, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            Guard.Against.Null(series, nameof(series));
            Guard.Against.Null(retrievedInstance, nameof(retrievedInstance));

            var count = retrievedInstance.Count;
            foreach (var instance in series.Instances)
            {
                foreach (var sopInstanceUid in instance.SopInstanceUid)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    _logger.RetrievingInstanceWithWado(sopInstanceUid);
                    var file = await dicomWebClient.Wado.Retrieve(studyInstanceUid, series.SeriesInstanceUid, sopInstanceUid).ConfigureAwait(false);
                    if (file is null) continue;

                    var uids = _dicomToolkit.GetStudySeriesSopInstanceUids(file);
                    if (retrievedInstance.ContainsKey(uids.Identifier))
                    {
                        _logger.InstanceAlreadyExists(uids.Identifier);
                        continue;
                    }

                    var dicomFileStorageMetadata = SaveFile(transactionId, file, uids);
                    await dicomFileStorageMetadata.SetDataStreams(file, file.ToJson(_options.Value.Dicom.WriteDicomJson)).ConfigureAwait(false);
                    retrievedInstance.Add(uids.Identifier, dicomFileStorageMetadata);
                    count++;
                }
            }
        }

        private async Task SaveFiles(string transactionId, IAsyncEnumerable<DicomFile> files, Dictionary<string, FileStorageMetadata> retrievedInstance, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(files, nameof(files));
            Guard.Against.Null(retrievedInstance, nameof(retrievedInstance));

            var count = retrievedInstance.Count;
            await foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                count++;

                var uids = _dicomToolkit.GetStudySeriesSopInstanceUids(file);
                if (retrievedInstance.ContainsKey(uids.Identifier))
                {
                    _logger.InstanceAlreadyExists(uids.Identifier);
                    continue;
                }

                var dicomFileStorageMetadata = SaveFile(transactionId, file, uids);
                await dicomFileStorageMetadata.SetDataStreams(file, file.ToJson(_options.Value.Dicom.WriteDicomJson)).ConfigureAwait(false);
                retrievedInstance.Add(uids.Identifier, dicomFileStorageMetadata);
            }
        }

        private DicomFileStorageMetadata SaveFile(string transactionId, DicomFile file, StudySerieSopUids uids)
        {
            Guard.Against.Null(transactionId, nameof(transactionId));
            Guard.Against.Null(file, nameof(file));

            return new DicomFileStorageMetadata(transactionId, uids.Identifier, uids.StudyInstanceUid, uids.SeriesInstanceUid, uids.SopInstanceUid)
            {
                CalledAeTitle = string.Empty,
                Source = transactionId,
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _rootScope.Dispose();
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

        #endregion Data Retrieval
    }
}
