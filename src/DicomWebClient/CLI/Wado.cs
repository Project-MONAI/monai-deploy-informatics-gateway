// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.CLI
{
    [Command("wado", "Use wado to retrieve DICOM studies, series, instances, etc...")]
    public class Wado : ConsoleAppBase
    {
        private readonly IDicomWebClient _dicomWebClient;
        private readonly ILogger<Wado> _logger;

        public Wado(IDicomWebClient dicomWebClient, ILogger<Wado> logger)
        {
            _dicomWebClient = dicomWebClient ?? throw new ArgumentNullException(nameof(dicomWebClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Command("study", "Retrieves instances within a study")]
        public async Task Study(
            [Option("r", "Uniform Resource Locator (URL) of the DICOMweb service")] string rootUrl,
            [Option("u", "username for authentication with the DICOMweb service")] string username,
            [Option("p", "password for authentication with the DICOMweb service")] string password,
            [Option("s", "unique study identifier; Study Instance UID")] string studyInstanceUid,
            [Option("f", "output format: json, dicom")] OutputFormat format = OutputFormat.Dicom,
            [Option("o", "output directory, default: current directory(.)", DefaultValue = ".")] string outputDir = ".",
            [Option("t", "transfer syntaxes, separated by comma")] string transferSyntaxes = "*"
            )
        {
            ValidateOptions(rootUrl, transferSyntaxes, out var rootUri, out var dicomTransferSyntaxes);
            ValidateOutputDirectory(ref outputDir);

            _dicomWebClient.ConfigureServiceUris(rootUri);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                _dicomWebClient.ConfigureAuthentication(Utils.GenerateFromUsernamePassword(username, password));
            }
            _logger.LogInformation($"Retrieving study {studyInstanceUid}...");
            if (format == OutputFormat.Dicom)
            {
                await SaveFiles(outputDir, _dicomWebClient.Wado.Retrieve(studyInstanceUid, transferSyntaxes: dicomTransferSyntaxes.ToArray())).ConfigureAwait(false);
            }
            else
            {
                await SaveJson(outputDir, _dicomWebClient.Wado.RetrieveMetadata<string>(studyInstanceUid)).ConfigureAwait(false);
            }
        }

        [Command("series", "Retrieves instances within a series")]
        public async Task Series(
            [Option("r", "Uniform Resource Locator (URL) of the DICOMweb service")] string rootUrl,
            [Option("s", "unique study identifier; Study Instance UID")] string studyInstanceUid,
            [Option("e", "unique series identifier; Series Instance UID")] string seriesInstanceUid,
            [Option("u", "username for authentication with the DICOMweb service")] string username = "",
            [Option("p", "password for authentication with the DICOMweb service")] string password = "",
            [Option("f", "output format: json, dicom")] OutputFormat format = OutputFormat.Dicom,
            [Option("o", "output directory, default: current directory(.)", DefaultValue = ".")] string outputDir = ".",
            [Option("t", "transfer syntaxes, separated by comma")] string transferSyntaxes = "*"
            )
        {
            ValidateOptions(rootUrl, transferSyntaxes, out var rootUri, out var dicomTransferSyntaxes);
            ValidateOutputDirectory(ref outputDir);

            _dicomWebClient.ConfigureServiceUris(rootUri);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                _dicomWebClient.ConfigureAuthentication(Utils.GenerateFromUsernamePassword(username, password));
            }
            _logger.LogInformation($"Retrieving series  {seriesInstanceUid} from");
            _logger.LogInformation($"\tStudy Instance UID: {studyInstanceUid}");
            if (format == OutputFormat.Dicom)
            {
                await SaveFiles(outputDir, _dicomWebClient.Wado.Retrieve(studyInstanceUid, seriesInstanceUid, transferSyntaxes: dicomTransferSyntaxes.ToArray())).ConfigureAwait(false);
            }
            else
            {
                await SaveJson(outputDir, _dicomWebClient.Wado.RetrieveMetadata<string>(studyInstanceUid, seriesInstanceUid)).ConfigureAwait(false);
            }
        }

        [Command("instance", "Retrieves an instance")]
        public async Task Instance(
            [Option("r", "Uniform Resource Locator (URL) of the DICOMweb service")] string rootUrl,
            [Option("s", "unique study identifier; Study Instance UID")] string studyInstanceUid,
            [Option("e", "unique series identifier; Series Instance UID")] string seriesInstanceUid,
            [Option("i", "unique instance identifier; SOP Instance UID")] string sopInstanceUid,
            [Option("u", "username for authentication with the DICOMweb service")] string username = "",
            [Option("p", "password for authentication with the DICOMweb service")] string password = "",
            [Option("f", "output format: json, dicom")] OutputFormat format = OutputFormat.Dicom,
            [Option("o", "output directory, default: current directory(.)", DefaultValue = ".")] string outputDir = ".",
            [Option("t", "transfer syntaxes, separated by comma")] string transferSyntaxes = "*"
            )
        {
            ValidateOptions(rootUrl, transferSyntaxes, out var rootUri, out var dicomTransferSyntaxes);
            ValidateOutputDirectory(ref outputDir);

            _dicomWebClient.ConfigureServiceUris(rootUri);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                _dicomWebClient.ConfigureAuthentication(Utils.GenerateFromUsernamePassword(username, password));
            }
            _logger.LogInformation($"Retrieving instance {sopInstanceUid} from");
            _logger.LogInformation($"\tStudy Instance UID: {studyInstanceUid}");
            _logger.LogInformation($"\tSeries Instance UID: {seriesInstanceUid}");

            if (format == OutputFormat.Dicom)
            {
                var file = await _dicomWebClient.Wado.Retrieve(studyInstanceUid, seriesInstanceUid, sopInstanceUid, transferSyntaxes: dicomTransferSyntaxes.ToArray()).ConfigureAwait(false);
                await Utils.SaveFiles(_logger, outputDir, file).ConfigureAwait(false);
            }
            else
            {
                var json = await _dicomWebClient.Wado.RetrieveMetadata<string>(studyInstanceUid, seriesInstanceUid, sopInstanceUid).ConfigureAwait(false);
                await Utils.SaveJson(_logger, outputDir, json, DicomTag.SOPInstanceUID).ConfigureAwait(false);
            }
        }

        [Command("bulk", "Retrieves bulkdata of an instance")]
        public async Task Bulk(
            [Option("r", "Uniform Resource Locator (URL) of the DICOMweb service")] string rootUrl,
            [Option("s", "unique study identifier; Study Instance UID")] string studyInstanceUid,
            [Option("e", "unique series identifier; Series Instance UID")] string seriesInstanceUid,
            [Option("i", "unique instance identifier; SOP Instance UID")] string sopInstanceUid,
            [Option("g", "DICOM tag containing the bulkdata")] string tag,
            [Option("u", "username for authentication with the DICOMweb service")] string username = "",
            [Option("p", "password for authentication with the DICOMweb service")] string password = "",
            [Option("o", "output filename", DefaultValue = ".")] string filename = "bulkdata.bin",
            [Option("t", "transfer syntaxes, separated by comma")] string transferSyntaxes = "*"
            )
        {
            ValidateOptions(rootUrl, transferSyntaxes, out var rootUri, out var dicomTransferSyntaxes);
            ValidateOutputFilename(ref filename);
            var dicomTag = DicomTag.Parse(tag);

            _dicomWebClient.ConfigureServiceUris(rootUri);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                _dicomWebClient.ConfigureAuthentication(Utils.GenerateFromUsernamePassword(username, password));
            }
            _logger.LogInformation($"Retrieving {dicomTag} from");
            _logger.LogInformation($"\tStudy Instance UID: {studyInstanceUid}");
            _logger.LogInformation($"\tSeries Instance UID: {seriesInstanceUid}");
            _logger.LogInformation($"\tSOP Instance UID: {sopInstanceUid}");
            var data = await _dicomWebClient.Wado.Retrieve(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTag, transferSyntaxes: dicomTransferSyntaxes.ToArray()).ConfigureAwait(false);

            _logger.LogInformation($"Saving data to {filename}....");
            await File.WriteAllBytesAsync(filename, data).ConfigureAwait(false);
        }

        private async Task SaveJson(string outputDir, IAsyncEnumerable<string> enumerable)
        {
            Guard.Against.NullOrWhiteSpace(outputDir, nameof(outputDir));
            Guard.Against.Null(enumerable, nameof(enumerable));

            await foreach (var item in enumerable)
            {
                await Utils.SaveJson(_logger, outputDir, item, DicomTag.SOPInstanceUID).ConfigureAwait(false);
            }
        }

        private async Task SaveFiles(string outputDir, IAsyncEnumerable<DicomFile> enumerable)
        {
            Guard.Against.NullOrWhiteSpace(outputDir, nameof(outputDir));
            Guard.Against.Null(enumerable, nameof(enumerable));

            var count = 0;
            await foreach (var file in enumerable)
            {
                await Utils.SaveFiles(_logger, outputDir, file).ConfigureAwait(false);
                count++;
            }
            _logger.LogInformation($"Successfully saved {count} files.");
        }

        private void ValidateOutputFilename(ref string filename)
        {
            Guard.Against.NullOrWhiteSpace(filename, nameof(filename));

            try
            {
                filename = Path.GetFullPath(filename);
            }
            catch (Exception ex)
            {
                throw new Exception($"-o output filename specified may be invalid or you do not have access to the path.", ex);
            }
            Utils.CheckAndConfirmOverwriteOutputFilename(_logger, filename);
        }

        private void ValidateOutputDirectory(ref string outputDir)
        {
            Guard.Against.NullOrWhiteSpace(outputDir, nameof(outputDir));

            if (outputDir == ".")
            {
                outputDir = Environment.CurrentDirectory;
            }
            else
            {
                Utils.CheckAndConfirmOverwriteOutput(_logger, outputDir);
            }
        }

        private void ValidateOptions(string rootUrl, string transferSyntaxes, out Uri rootUri, out List<DicomTransferSyntax> dicomTransferSyntaxes)
        {
            Guard.Against.NullOrWhiteSpace(rootUrl, nameof(rootUrl));
            Guard.Against.NullOrWhiteSpace(transferSyntaxes, nameof(transferSyntaxes));

            _logger.LogInformation("Checking arguments...");
            rootUri = new Uri(rootUrl);
            rootUri = rootUri.EnsureUriEndsWithSlash();

            dicomTransferSyntaxes = new List<DicomTransferSyntax>();
            var transferSyntaxArray = transferSyntaxes.Split(',');
            foreach (var uid in transferSyntaxArray)
            {
                var uidData = DicomUID.Parse(uid, type: DicomUidType.TransferSyntax);
                if (uidData.Name.Equals("Unknown") && uidData.UID != "*")
                {
                    throw new ArgumentException($"Invalid transfer syntax: {uid}");
                }
                dicomTransferSyntaxes.Add(DicomTransferSyntax.Parse(uidData.UID));
            }
        }
    }
}
