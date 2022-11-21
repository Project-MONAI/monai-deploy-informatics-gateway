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
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class DicomCStoreDataClient : IDataClient
    {
        private static readonly object SyncRoot = new object();
        internal int TotalTime { get; private set; } = 0;

        private readonly Configurations _configurations;
        private readonly InformaticsGatewayConfiguration _options;
        private readonly ISpecFlowOutputHelper _outputHelper;

        public DicomCStoreDataClient(Configurations configurations, InformaticsGatewayConfiguration options, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }


        public async Task SendAsync(DataProvider dataProvider, params object[] args)
        {
            Guard.Against.NullOrEmpty(args);

            var callingAeTitle = args[0].ToString();
            var host = args[1].ToString();
            var port = (int)args[2];
            var calledAeTitle = args[3].ToString();
            var timeout = (TimeSpan)args[4];

            _outputHelper.WriteLine($"C-STORE: {callingAeTitle} => {host}:{port}@{calledAeTitle}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var dicomClient = DicomClientFactory.Create(host, port, false, callingAeTitle, calledAeTitle);
            var countdownEvent = new CountdownEvent(dataProvider.DicomSpecs.Files.Count);
            var failureStatus = new List<DicomStatus>();
            foreach (var file in dataProvider.DicomSpecs.Files)
            {
                var cStoreRequest = new DicomCStoreRequest(file);
                cStoreRequest.OnResponseReceived += (DicomCStoreRequest request, DicomCStoreResponse response) =>
                {
                    if (response.Status != DicomStatus.Success) failureStatus.Add(response.Status);
                    countdownEvent.Signal();
                };
                await dicomClient.AddRequestAsync(cStoreRequest);
            }

            try
            {
                await dicomClient.SendAsync();
                countdownEvent.Wait(timeout);
                stopwatch.Stop();
                lock (SyncRoot)
                {
                    TotalTime += (int)stopwatch.Elapsed.TotalMilliseconds;
                }
                _outputHelper.WriteLine($"DICOMsend:{stopwatch.Elapsed.TotalSeconds}s");
            }
            catch (DicomAssociationRejectedException ex)
            {
                _outputHelper.WriteLine($"Association Rejected: {ex.Message}");
                dataProvider.DimseRsponse = DicomStatus.Cancel;
            }

            dataProvider.DimseRsponse = (failureStatus.Count == 0) ? DicomStatus.Success : failureStatus.First();
        }
    }
}
