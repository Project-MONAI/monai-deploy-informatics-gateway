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
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Database;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Hooks
{
    [Binding]
    public sealed class SqlHooks
    {
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly ScenarioContext _scenarioContext;
        private readonly InformaticsGatewayContext _dbContext;

        public SqlHooks(ISpecFlowOutputHelper outputHelper, Configurations configuration, ScenarioContext scenarioContext)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));

            var dbPath = Path.Combine(Environment.GetEnvironmentVariable("SCRIPT_DIR"), ".run", "ig", "database", "mig.db");
            _outputHelper.WriteLine("DICOM Adapter Database: {0}", dbPath);

            var builder = new DbContextOptionsBuilder<InformaticsGatewayContext>();
            builder.UseSqlite($"Data Source={dbPath}");
            _dbContext = new InformaticsGatewayContext(builder.Options);
        }

        [BeforeScenario("@sql_inject_acr_request")]
        public async Task BeforeMessagingExportComplete(ISpecFlowOutputHelper outputHelper, ScenarioContext scenarioContext)
        {
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                State = InferenceRequestState.InProcess,
                StoragePath = "na",
                OutputResources = new List<RequestOutputDataResource>()
                {
                    new RequestOutputDataResource
                    {
                        Interface = InputInterfaceType.DicomWeb,
                        ConnectionDetails = new DicomWebConnectionDetails
                        {
                            Uri = _configuration.OrthancOptions.DicomWebRootInternal,
                            AuthId = _configuration.OrthancOptions.GetBase64EncodedAuthHeader(),
                            AuthType = ConnectionAuthType.Basic
                        }
                    }
                }
            };
            _dbContext.Add(request);
            await _dbContext.SaveChangesAsync();
            scenarioContext[DicomDimseScuServicesStepDefinitions.KeyDestination] = request.TransactionId;
            outputHelper.WriteLine($"Injected ACR request {request.TransactionId}");
        }
    }
}
