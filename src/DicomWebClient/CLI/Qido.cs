/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2020 NVIDIA Corporation
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
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.CLI
{
    [Command("qido", "Use qido to query DICOM studies, series, instances, etc...")]
    public class Qido : ConsoleAppBase
    {
        private readonly IDicomWebClient _dicomWebClient;
        private readonly ILogger<Qido> _logger;

        public Qido(IDicomWebClient dicomWebClient, ILogger<Qido> logger)
        {
            _dicomWebClient = dicomWebClient ?? throw new ArgumentNullException(nameof(dicomWebClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Command("study", "Retrieves instances within a study")]
        public async Task Study(
            [Option("r", "Uniform Resource Locator (URL) of the DICOMweb service")] string rootUrl,
            [Option("q", "query parameters. e.g. \"PatientID=Bob*,00080060=CT\"")] string query,
            [Option("u", "username for authentication with the DICOMweb service")] string username = "",
            [Option("p", "password for authentication with the DICOMweb service")] string password = "",
            [Option("d", "fields to include. e.g. [PatientName, 00080020].")] List<string> fieldsToInclude = null,
            [Option("f", "fuzzy matching")] bool fuzzyMatching = false,
            [Option("o", "output directory, default: current directory(.)", DefaultValue = ".")] string outputDir = "."
            )
        {
            ValidateOptions(rootUrl, out var rootUri);
            ValidateOutputDirectory(ref outputDir);

            _dicomWebClient.ConfigureServiceUris(rootUri);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                _dicomWebClient.ConfigureAuthentication(Utils.GenerateFromUsernamePassword(username, password));
            }
            _logger.LogInformation($"Querying studies...");

            var queryParameters = ParseQueryString(query);

            await SaveJson(outputDir, _dicomWebClient.Qido.SearchForStudies<string>(queryParameters, fieldsToInclude, fuzzyMatching)).ConfigureAwait(false);
        }

        private Dictionary<string, string> ParseQueryString(string query)
        {
            var pairs = query.Split(",", StringSplitOptions.RemoveEmptyEntries);
            var queryParameters = new Dictionary<string, string>();
            foreach (var pair in pairs)
            {
                var parts = pair.Split("=", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    _logger.LogError($"Unknown query parameter {pair}");
                }
                if (queryParameters.ContainsKey(parts[0]))
                {
                    throw new Exception($"Query key already exists {parts[0]}");
                }
                queryParameters.Add(parts[0], parts[1]);
            }
            return queryParameters;
        }

        private async Task SaveJson(string outputDir, IAsyncEnumerable<string> enumerable)
        {
            Guard.Against.NullOrWhiteSpace(outputDir, nameof(outputDir));
            Guard.Against.Null(enumerable, nameof(enumerable));

            await foreach (var item in enumerable)
            {
                await Utils.SaveJson(_logger, outputDir, item, DicomTag.StudyInstanceUID).ConfigureAwait(false);
            }
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

        private void ValidateOptions(string rootUrl, out Uri rootUri)
        {
            Guard.Against.NullOrWhiteSpace(rootUrl, nameof(rootUrl));

            _logger.LogInformation("Checking arguments...");
            rootUri = new Uri(rootUrl);
            rootUri = rootUri.EnsureUriEndsWithSlash();
        }
    }
}
