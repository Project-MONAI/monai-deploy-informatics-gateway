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

using BoDi;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Hooks
{
    public sealed class EfDataProvider : IDatabaseDataProvider
    {
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly ObjectContainer _objectContainer;
        private readonly InformaticsGatewayContext _dbContext;

        public EfDataProvider(ISpecFlowOutputHelper outputHelper, Configurations configuration, InformaticsGatewayContext informaticsGatewayContext)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dbContext = informaticsGatewayContext ?? throw new ArgumentNullException(nameof(informaticsGatewayContext)); ;
        }

        public async Task<string> InjectAcrRequest()
        {
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                State = InferenceRequestState.InProcess,
                OutputResources = new List<RequestOutputDataResource>()
                {
                    new RequestOutputDataResource
                    {
                        Interface = InputInterfaceType.DicomWeb,
                        ConnectionDetails = new DicomWebConnectionDetails
                        {
                            Uri = _configuration.OrthancOptions.DicomWebRoot,
                            AuthId = _configuration.OrthancOptions.GetBase64EncodedAuthHeader(),
                            AuthType = ConnectionAuthType.Basic
                        }
                    }
                }
            };
            _dbContext.Add(request);
            await _dbContext.SaveChangesAsync();
            _outputHelper.WriteLine($"Injected ACR request {request.TransactionId}");
            return request.TransactionId;
        }
    }
}
