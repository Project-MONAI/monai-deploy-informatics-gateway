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

using Ardalis.GuardClauses;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class DicomCEchoDataClient : IDataClient
    {
        private readonly Configurations _configurations;
        private readonly InformaticsGatewayConfiguration _options;
        private readonly ISpecFlowOutputHelper _outputHelper;

        public DicomCEchoDataClient(Configurations configurations, InformaticsGatewayConfiguration options, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public async Task SendAsync(DataProvider dataProvider, params object[] args)
        {
            Guard.Against.NullOrEmpty(args, nameof(args));

            var callingAeTitle = args[0].ToString();
            var host = args[1].ToString();
            var port = (int)args[2];
            var calledAeTitle = args[3].ToString();
            var timeout = (TimeSpan)args[4];

            _outputHelper.WriteLine($"C-ECHO: {callingAeTitle} => {host}:{port}@{calledAeTitle}");
            var result = DicomStatus.Pending;
            var dicomClient = DicomClientFactory.Create(host, port, false, callingAeTitle, calledAeTitle);

            var cEchoRequest = new DicomCEchoRequest();
            var manualReset = new ManualResetEvent(false);
            cEchoRequest.OnResponseReceived += (DicomCEchoRequest request, DicomCEchoResponse response) =>
            {
                result = response.Status;
                manualReset.Set();
            };
            await dicomClient.AddRequestAsync(cEchoRequest);

            try
            {
                await dicomClient.SendAsync();
                manualReset.WaitOne(timeout);
                dataProvider.DimseRsponse = result;
            }
            catch (DicomAssociationRejectedException ex)
            {
                _outputHelper.WriteLine("Association Rejected: {0}", ex.Message);
                dataProvider.DimseRsponse = DicomStatus.Cancel;
            }
        }
        public Task SaveHl7Async(DataProvider dataProvider, params object[] args) => throw new NotImplementedException();
    }
}
