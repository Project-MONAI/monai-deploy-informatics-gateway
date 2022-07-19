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
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    internal class TemporaryFileStore : ITemporaryFileStore
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<TemporaryFileStore> _logger;
        private readonly IDicomToolkit _dicomToolkit;
        private readonly DicomJsonOptions _dicomJsonOption;
        private readonly StorageConfiguration _storageConfiguration;

        public TemporaryFileStore(
            IFileSystem fileSystem,
            ILogger<TemporaryFileStore> logger,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IDicomToolkit dicomToolkit)
        {
            Guard.Against.Null(configuration, nameof(configuration));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
            _dicomJsonOption = configuration.Value.Dicom.WriteDicomJson;
            _storageConfiguration = configuration.Value.Storage;
        }

        public IReadOnlyList<FileStoragePath> RestoreInferenceRequestFiles(string transactionId, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));

            var files = new List<FileStoragePath>();

            RestoreDicomFiles(files, transactionId, cancellationToken);
            RestoreFhirFiles(files, transactionId, cancellationToken);

            return files;
        }

        private void RestoreFhirFiles(List<FileStoragePath> files, string transactionId, CancellationToken cancellationToken)
        {
            Guard.Against.Null(files, nameof(files));
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));

            var scanPath = _fileSystem.Path.Combine(_storageConfiguration.TemporaryDataDirFullPath, transactionId, FhirFileStorageInfo.FhirSubDirectoryName);

            if (!_fileSystem.Directory.Exists(scanPath))
            {
                _logger.DirectoryDoesNotExistsNoFilesRestored(scanPath);
                return;
            }

            foreach (var file in _fileSystem.Directory.EnumerateFiles(scanPath, "*", System.IO.SearchOption.AllDirectories))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var parts = _fileSystem.Path.GetFileNameWithoutExtension(file).Split('-');

                if (parts.Length < 2)
                {
                    _logger.UnableToRestoreFile(file);
                    continue;
                }
                var filePath = new FhirStoragePath { FilePath = file, ResourceType = parts[0], ResourceId = parts[1] };

                files.Add(filePath);
            }
        }

        private void RestoreDicomFiles(List<FileStoragePath> files, string transactionId, CancellationToken cancellationToken)
        {
            Guard.Against.Null(files, nameof(files));
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));

            var scanPath = _fileSystem.Path.Combine(_storageConfiguration.TemporaryDataDirFullPath, transactionId, DicomFileStorageInfo.DicomSubDirectoryName);

            if (!_fileSystem.Directory.Exists(scanPath))
            {
                _logger.DirectoryDoesNotExistsNoFilesRestored(scanPath);
                return;
            }

            foreach (var file in _fileSystem.Directory.EnumerateFiles(scanPath, "*", System.IO.SearchOption.AllDirectories))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (_dicomToolkit.HasValidHeader(file))
                {
                    var uids = _dicomToolkit.GetStudySeriesSopInstanceUids(file);
                    var filePaths = new DicomStoragePaths { FilePath = file, UIDs = uids };
                    var jsonFile = $"{file}{DicomFileStorageInfo.DicomJsonFileExtension}";
                    if (_fileSystem.File.Exists(jsonFile))
                    {
                        filePaths.DicomMetadataFilePath = jsonFile;
                    }
                    files.Add(filePaths);
                }
                else
                {
                    _logger.SkippingNoneDicomFiles(file);
                }
            }
        }

        public async Task<DicomStoragePaths> SaveDicomInstance(string transactionId, DicomFile dicomFile, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.Null(dicomFile, nameof(dicomFile));

            var filePath = string.Empty;
            var jsonFilePath = string.Empty;

            return await Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(
                   _storageConfiguration.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.ErrorSavingInstance(filePath, retryCount, exception);
                   })
               .ExecuteAsync<DicomStoragePaths>(async (cancellationToken) =>
                {
                    filePath = _fileSystem.Path.Combine(_storageConfiguration.TemporaryDataDirFullPath, transactionId, DicomFileStorageInfo.DicomSubDirectoryName, $"{Guid.NewGuid()}{DicomFileStorageInfo.FilExtension}");
                    jsonFilePath = $"{filePath}{DicomFileStorageInfo.DicomJsonFileExtension}";
                    var uids = _dicomToolkit.GetStudySeriesSopInstanceUids(dicomFile);

                    _fileSystem.Directory.CreateDirectoryIfNotExists(_fileSystem.Path.GetDirectoryName(filePath));

                    _logger.SavingDicomFile(filePath, jsonFilePath);
                    await _dicomToolkit.Save(dicomFile, filePath, jsonFilePath, _dicomJsonOption).ConfigureAwait(false);
                    _logger.FileSaved(filePath);

                    return new DicomStoragePaths { FilePath = filePath, DicomMetadataFilePath = jsonFilePath, UIDs = uids };
                }, cancellationToken)
               .ConfigureAwait(false);
        }

        public async Task<FhirStoragePath> SaveFhirResource(string transactionId, string resourceType, string resourceId, FhirStorageFormat fhirFormat, string data, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.NullOrWhiteSpace(resourceType, nameof(resourceType));
            Guard.Against.NullOrWhiteSpace(resourceId, nameof(resourceId));
            Guard.Against.NullOrWhiteSpace(data, nameof(data));

            var filePath = string.Empty;

            return await Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(
                   _storageConfiguration.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.ErrorSavingInstance(filePath, retryCount, exception);
                   })
               .ExecuteAsync<FhirStoragePath>(async (cancellationToken) =>
               {
                   var fileExtension = fhirFormat == FhirStorageFormat.Json ? FhirFileStorageInfo.JsonFilExtension : FhirFileStorageInfo.XmlFilExtension;
                   var dir = _fileSystem.Path.Combine(_storageConfiguration.TemporaryDataDirFullPath, transactionId, FhirFileStorageInfo.FhirSubDirectoryName);
                   filePath = _fileSystem.Path.Combine(dir, $"{resourceType}-{resourceId}-{Guid.NewGuid()}{fileExtension}");

                   _fileSystem.Directory.CreateDirectoryIfNotExists(_fileSystem.Path.GetDirectoryName(filePath));

                   _logger.SavingFile(filePath);
                   await _fileSystem.File.WriteAllTextAsync(filePath, data, cancellationToken).ConfigureAwait(false);
                   _logger.FileSaved(filePath);

                   return new FhirStoragePath { FilePath = filePath, ResourceType = resourceType, ResourceId = resourceId };
               }, cancellationToken: cancellationToken)
               .ConfigureAwait(false);
        }
    }
}
