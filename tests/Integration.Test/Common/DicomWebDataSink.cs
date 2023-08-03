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

using System.Diagnostics;
using System.Net;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class DicomWebDataClient : IDataClient
    {
        private readonly Configurations _configurations;
        private readonly InformaticsGatewayConfiguration _options;
        private readonly ISpecFlowOutputHelper _outputHelper;

        public DicomWebDataClient(Configurations configurations, InformaticsGatewayConfiguration options, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        /// <summary>
        /// args:
        ///  0: endpoint
        ///  1: workflows
        ///  2: callback function
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task SendAsync(DataProvider dataProvider, params object[] args)
        {
            Guard.Against.Null(dataProvider, nameof(dataProvider));
            Guard.Against.Null(args, nameof(args));

            var dicomFileSpec = dataProvider.DicomSpecs;
            dicomFileSpec.Should().NotBeNull();
            dicomFileSpec.StudyInstanceUids.Should().NotBeNullOrEmpty();

            var endpoint = args[0].ToString();

            _outputHelper.WriteLine($"POSTing studies to {endpoint} with {dicomFileSpec.Files.Count} files...");
            var httpClient = HttpClientFactory.Create();
            var dicomWebClient = new DicomWebClient(httpClient, null);
            dicomWebClient.ConfigureServiceUris(new Uri(endpoint));

            if (args[1] is not null)
            {
                var workflows = args[1] as string[];
                workflows.Should().NotBeNullOrEmpty();
                dicomWebClient.ConfigureServicePrefix(DicomWebServiceType.Stow, $"{workflows.First()}/");
                _outputHelper.WriteLine($"configured STOW service prefix = {workflows.First()}...");
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var func = args[2] as Func<DicomWebClient, DicomDataSpecs, Task<DicomWebResponse<string>>>;
            var results = await func(dicomWebClient, dicomFileSpec);
            results.StatusCode.Should().Be(HttpStatusCode.OK);

            stopwatch.Stop();
            _outputHelper.WriteLine($"Time to upload to DICOMWeb={0}s...", stopwatch.Elapsed.TotalSeconds);
        }
    }
}
