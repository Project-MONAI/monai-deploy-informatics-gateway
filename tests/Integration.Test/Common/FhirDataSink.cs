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
using System.Text;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class FhirDataClient : IDataClient
    {
        private readonly Configurations _configurations;
        private readonly InformaticsGatewayConfiguration _options;
        private readonly ISpecFlowOutputHelper _outputHelper;

        public FhirDataClient(Configurations configurations, InformaticsGatewayConfiguration options, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public async Task SendAsync(DataProvider dataProvider, params object[] args)
        {
            Guard.Against.Null(dataProvider, nameof(dataProvider));
            var httpClient = HttpClientFactory.Create();
            httpClient.BaseAddress = new Uri($"{_configurations.InformaticsGatewayOptions.ApiEndpoint}/fhir/");
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(dataProvider.FhirSpecs.MediaType));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", dataProvider.FhirSpecs.MediaType);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var file in dataProvider.FhirSpecs.Files.Keys)
            {
                var path = Path.GetFileNameWithoutExtension(file);
                _outputHelper.WriteLine($"Sending file {file} to /fhir/{path}...");
                var httpContent = new StringContent(dataProvider.FhirSpecs.Files[file], Encoding.UTF8, dataProvider.FhirSpecs.MediaType);
                var response = await httpClient.PostAsync(path, httpContent);
                response.EnsureSuccessStatusCode();
            }

            stopwatch.Stop();
            _outputHelper.WriteLine($"Time to upload FHIR data={0}s...", stopwatch.Elapsed.TotalSeconds);
        }
    }
}
