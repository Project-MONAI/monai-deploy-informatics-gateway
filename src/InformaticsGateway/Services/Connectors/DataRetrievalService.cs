// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
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
    internal class DataRetrievalService : IHostedService, IMonaiService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DataRetrievalService> _logger;
        private readonly IStorageInfoProvider _storageInfoProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IDicomToolkit _dicomToolkit;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        public ServiceStatus Status { get; set; }

        public string ServiceName => "Data Retrieval Service";

        public DataRetrievalService(
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory,
            ILogger<DataRetrievalService> logger,
            IFileSystem fileSystem,
            IDicomToolkit dicomToolkit,
            IServiceScopeFactory serviceScopeFactory,
            IPayloadAssembler payloadAssembler,
            IStorageInfoProvider storageInfoProvider,
            IOptions<InformaticsGatewayConfiguration> options)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _payloadAssembler = payloadAssembler ?? throw new ArgumentNullException(nameof(payloadAssembler));
            _storageInfoProvider = storageInfoProvider ?? throw new ArgumentNullException(nameof(storageInfoProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(async () =>
            {
                await BackgroundProcessing(cancellationToken).ConfigureAwait(true);
            }, CancellationToken.None);

            Status = ServiceStatus.Running;
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
            _logger.ServiceRunning(ServiceName);

            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInferenceRequestRepository>();
                if (!_storageInfoProvider.HasSpaceAvailableToRetrieve)
                {
                    _logger.DataRetrievalPaused(_storageInfoProvider.AvailableFreeSpace);
                    await Task.Delay(500, cancellationToken).ConfigureAwait(true);
                    continue;
                }

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

            var retrievedFiles = new Dictionary<string, FileStorageInfo>(StringComparer.OrdinalIgnoreCase);
            RestoreExistingInstances(inferenceRequest, retrievedFiles);

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

            NotifyNewInstance(inferenceRequest, retrievedFiles);
        }

        private void NotifyNewInstance(InferenceRequest inferenceRequest, Dictionary<string, FileStorageInfo> retrievedFiles)
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
                _payloadAssembler.Queue(inferenceRequest.TransactionId, retrievedFiles[key]);
            }
        }

        private void RestoreExistingInstances(InferenceRequest inferenceRequest, Dictionary<string, FileStorageInfo> retrievedInstances)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));
            Guard.Against.Null(retrievedInstances, nameof(retrievedInstances));

            _logger.RestoringRetrievedFiles(inferenceRequest.StoragePath);
            foreach (var file in _fileSystem.Directory.EnumerateFiles(inferenceRequest.StoragePath, "*", System.IO.SearchOption.AllDirectories))
            {
                var instance = new FileStorageInfo { StorageRootPath = inferenceRequest.StoragePath, CorrelationId = inferenceRequest.TransactionId, FilePath = file };

                if (retrievedInstances.ContainsKey(instance.FilePath))
                {
                    continue;
                }
                retrievedInstances.Add(instance.FilePath, instance);
                _logger.RestoredFile(instance.FilePath);
            }
        }

        #region Data Retrieval

        private async Task RetrieveViaFhir(InferenceRequest inferenceRequest, RequestInputDataResource source, Dictionary<string, FileStorageInfo> retrievedResources, CancellationToken cancellationToken)
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
                await RetrieveFhirResources(inferenceRequest.TransactionId, input, source, retrievedResources, inferenceRequest.StoragePath).ConfigureAwait(false);
            }
        }

        private async Task RetrieveFhirResources(string transactionId, InferenceRequestDetails requestDetails, RequestInputDataResource source, Dictionary<string, FileStorageInfo> retrievedResources, string storagePath)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(requestDetails, nameof(requestDetails));
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(retrievedResources, nameof(retrievedResources));
            Guard.Against.NullOrWhiteSpace(storagePath, nameof(storagePath));

            var pendingResources = new Queue<FhirResource>(requestDetails.Resources.Where(p => !p.IsRetrieved));

            if (pendingResources.Count == 0)
            {
                return;
            }

            var authenticationHeaderValue = AuthenticationHeaderValueExtensions.ConvertFrom(source.ConnectionDetails.AuthType, source.ConnectionDetails.AuthId);

            var httpClient = _httpClientFactory.CreateClient("fhir");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = authenticationHeaderValue;
            _fileSystem.Directory.CreateDirectoryIfNotExists(storagePath);

            FhirResource resource = null;
            try
            {
                while (pendingResources.Count > 0)
                {
                    resource = pendingResources.Dequeue();
                    resource.IsRetrieved = await RetrieveFhirResource(
                        transactionId,
                        httpClient,
                        resource,
                        source,
                        retrievedResources,
                        storagePath,
                        requestDetails.FhirFormat,
                        requestDetails.FhirAcceptHeader).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorRetrievingFhirResource(resource?.Type, resource?.Id, ex);
                throw;
            }
        }

        private async Task<bool> RetrieveFhirResource(string transactionId, HttpClient httpClient, FhirResource resource, RequestInputDataResource source, Dictionary<string, FileStorageInfo> retrievedResources, string storagePath, FhirStorageFormat fhirFormat, string acceptHeader)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(httpClient, nameof(httpClient));
            Guard.Against.Null(resource, nameof(resource));
            Guard.Against.Null(source, nameof(source));
            Guard.Against.Null(retrievedResources, nameof(retrievedResources));
            Guard.Against.NullOrWhiteSpace(storagePath, nameof(storagePath));
            Guard.Against.NullOrWhiteSpace(acceptHeader, nameof(acceptHeader));

            _logger.RetrievingFhirResource(resource.Type, resource.Id, acceptHeader, fhirFormat);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{source.ConnectionDetails.Uri}{resource.Type}/{resource.Id}");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
            var response = await Policy
                .HandleResult<HttpResponseMessage>(p => !p.IsSuccessStatusCode)
                .WaitAndRetryAsync(3,
                    (retryAttempt) =>
                    {
                        return retryAttempt == 1 ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromMilliseconds(500);
                    },
                    (result, timeSpan, retryCount, context) =>
                    {
                        _logger.ErrorRetrievingFhirResourceWithRetry(resource.Type, resource.Id, result.Result.StatusCode, retryCount, result.Exception);
                    })
                .ExecuteAsync(async () => await httpClient.SendAsync(request).ConfigureAwait(false)).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var file = new FhirFileStorageInfo(transactionId, storagePath, resource.Id, fhirFormat, transactionId, _fileSystem) { ResourceType = resource.Type };
                _fileSystem.Directory.CreateDirectoryIfNotExists(_fileSystem.Path.GetDirectoryName(file.FilePath));
                await _fileSystem.File.WriteAllTextAsync(file.FilePath, json).ConfigureAwait(false);
                retrievedResources.Add(file.FilePath, file);
                return true;
            }
            else
            {
                _logger.ErrorRetrievingFhirResourceWithStatus(resource.Type, resource.Id, response.StatusCode);
                return false;
            }
        }

        private async Task RetrieveViaDicomWeb(InferenceRequest inferenceRequest, RequestInputDataResource source, Dictionary<string, FileStorageInfo> retrievedInstance, CancellationToken cancellationToken)
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
                        await RetrieveStudies(inferenceRequest.TransactionId, dicomWebClient, input.Studies, inferenceRequest.StoragePath, retrievedInstance, cancellationToken).ConfigureAwait(false);
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

        private async Task QueryStudies(string transactionId, DicomWebClient dicomWebClient, InferenceRequest inferenceRequest, Dictionary<string, FileStorageInfo> retrievedInstance, string dicomTag, string queryValue, CancellationToken cancellationToken)
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
                await RetrieveStudies(transactionId, dicomWebClient, studies, inferenceRequest.StoragePath, retrievedInstance, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.QidoCompletedWithNoResult(dicomTag, queryValue);
            }
        }

        private async Task RetrieveStudies(string transactionId, IDicomWebClient dicomWebClient, IList<RequestedStudy> studies, string storagePath, Dictionary<string, FileStorageInfo> retrievedInstance, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(studies, nameof(studies));
            Guard.Against.Null(storagePath, nameof(storagePath));
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
                    await SaveFiles(transactionId, files, storagePath, retrievedInstance, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await RetrieveSeries(transactionId, dicomWebClient, study, storagePath, retrievedInstance, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task RetrieveSeries(string transactionId, IDicomWebClient dicomWebClient, RequestedStudy study, string storagePath, Dictionary<string, FileStorageInfo> retrievedInstance, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(study, nameof(study));
            Guard.Against.Null(storagePath, nameof(storagePath));
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
                    await SaveFiles(transactionId, files, storagePath, retrievedInstance, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await RetrieveInstances(transactionId, dicomWebClient, study.StudyInstanceUid, series, storagePath, retrievedInstance, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task RetrieveInstances(string transactionId, IDicomWebClient dicomWebClient, string studyInstanceUid, RequestedSeries series, string storagePath, Dictionary<string, FileStorageInfo> retrievedInstance, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            Guard.Against.Null(series, nameof(series));
            Guard.Against.Null(storagePath, nameof(storagePath));
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
                    var fileStorageInfo = new DicomFileStorageInfo(transactionId, storagePath, count.ToString(CultureInfo.InvariantCulture), transactionId, _fileSystem);
                    PopulateHeaders(fileStorageInfo, file);
                    if (retrievedInstance.ContainsKey(fileStorageInfo.FilePath))
                    {
                        _logger.InstanceAlreadyExists(fileStorageInfo.FilePath);
                        continue;
                    }

                    SaveFile(file, fileStorageInfo);
                    retrievedInstance.Add(fileStorageInfo.FilePath, fileStorageInfo);
                    count++;
                }
            }
        }

        private async Task SaveFiles(string transactionId, IAsyncEnumerable<DicomFile> files, string storagePath, Dictionary<string, FileStorageInfo> retrievedInstance, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(files, nameof(files));
            Guard.Against.Null(storagePath, nameof(storagePath));
            Guard.Against.Null(retrievedInstance, nameof(retrievedInstance));

            var count = retrievedInstance.Count;
            await foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                count++;
                var instance = new DicomFileStorageInfo(transactionId, storagePath, count.ToString(CultureInfo.InvariantCulture), transactionId, _fileSystem);
                PopulateHeaders(instance, file);

                if (retrievedInstance.ContainsKey(instance.FilePath))
                {
                    _logger.InstanceAlreadyExists(instance.FilePath);
                    continue;
                }

                SaveFile(file, instance);
                retrievedInstance.Add(instance.FilePath, instance);
            }
        }

        private static void PopulateHeaders(DicomFileStorageInfo instance, DicomFile dicomFile)
        {
            instance.StudyInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            instance.SeriesInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            instance.SopInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
        }

        private void SaveFile(DicomFile file, DicomFileStorageInfo instanceStorageInfo)
        {
            Guard.Against.Null(file, nameof(file));
            Guard.Against.Null(instanceStorageInfo, nameof(instanceStorageInfo));

            Policy.Handle<Exception>()
                .WaitAndRetry(3,
                (retryAttempt) =>
                {
                    return retryAttempt == 1 ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromMilliseconds(500);
                },
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.ErrorSavingInstance(instanceStorageInfo.FilePath, retryCount, exception);
                })
                .Execute(() =>
                {
                    _logger.SavingInstance(instanceStorageInfo.FilePath);
                    _dicomToolkit.Save(file, instanceStorageInfo.FilePath, instanceStorageInfo.DicomJsonFilePath, _options.Value.Dicom.WriteDicomJson);
                    _logger.InstanceSaved(instanceStorageInfo.FilePath);
                });
        }

        #endregion Data Retrieval
    }
}
