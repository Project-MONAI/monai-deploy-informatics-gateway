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
using Minio;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class MinioDataClient : IDataClient
    {
        private readonly Configurations _configurations;
        private readonly InformaticsGatewayConfiguration _options;
        private readonly ISpecFlowOutputHelper _outputHelper;

        public MinioDataClient(Configurations configurations, InformaticsGatewayConfiguration options, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public async Task SendAsync(DataProvider dataProvider, params object[] args)
        {
            var minioClient = new MinioClient()
                .WithEndpoint(_options.Storage.Settings["endpoint"])
                .WithCredentials(_options.Storage.Settings["accessKey"], _options.Storage.Settings["accessToken"])
            .Build();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _outputHelper.WriteLine($"Uploading {dataProvider.DicomSpecs.FileCount} files to MinIO...");

            foreach (var file in dataProvider.DicomSpecs.Files)
            {
                var filename = file.GenerateFileName();
                var stream = new MemoryStream();
                await file.SaveAsync(stream);
                stream.Position = 0;
                var puObjectArgs = new PutObjectArgs();
                puObjectArgs.WithBucket(_options.Storage.StorageServiceBucketName)
                    .WithObject(filename)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length);
                await minioClient.PutObjectAsync(puObjectArgs);
            }

            stopwatch.Stop();
            _outputHelper.WriteLine($"Time to upload to Minio={0}s...", stopwatch.Elapsed.TotalSeconds);
        }
    }
}
