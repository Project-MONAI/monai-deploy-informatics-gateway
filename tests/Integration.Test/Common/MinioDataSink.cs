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
using Minio;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Polly;
using Polly.Retry;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class MinioDataClient : IDataClient
    {
        private readonly Configurations _configurations;
        private readonly InformaticsGatewayConfiguration _options;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly AsyncRetryPolicy _retryPolicy;

        public MinioDataClient(Configurations configurations, InformaticsGatewayConfiguration options, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(retryCount: 5, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(500));
        }

        public async Task SendAsync(DataProvider dataProvider, params object[] args)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var minioClient = CreateMinioClient();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                _outputHelper.WriteLine($"Uploading {dataProvider.DicomSpecs.FileCount} files to MinIO...");

                foreach (var file in dataProvider.DicomSpecs.Files.Values)
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
            });
        }

        public async Task SaveHl7Async(DataProvider dataProvider, params object[] args)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var minioClient = CreateMinioClient();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                _outputHelper.WriteLine($"Uploading {dataProvider.HL7Specs.Files.Count} files to MinIO...");

                foreach (var key in dataProvider.HL7Specs.Files.Keys)
                {
                    var file = dataProvider.HL7Specs.Files[key];
                    var filename = $"{args[0]}/{key.Replace(".txt", ".hl7")}";
                    var byteArray = Encoding.ASCII.GetBytes(file.HL7Message);
                    var stream = new MemoryStream(byteArray);

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
            });
        }

        private MinioClient CreateMinioClient() => new MinioClient()
                        .WithEndpoint(_options.Storage.Settings["endpoint"])
                        .WithCredentials(_options.Storage.Settings["accessKey"], _options.Storage.Settings["accessToken"])
                    .Build();

        internal void CleanBucketAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var count = 0;
            var minioClient = CreateMinioClient();
            var listObjectArgs = new ListObjectsArgs()
                .WithBucket(_options.Storage.StorageServiceBucketName)
                .WithRecursive(true);

            var reset = new ManualResetEventSlim();
            var objects = minioClient.ListObjectsAsync(listObjectArgs);
            objects.Subscribe(async (item) =>
            {
                var deletObjectsArgs = new RemoveObjectArgs()
                    .WithBucket(_options.Storage.StorageServiceBucketName)
                    .WithObject(item.Key);
                await minioClient.RemoveObjectAsync(deletObjectsArgs).ConfigureAwait(false);
                count++;
            },
            ex => Console.WriteLine($"Error listing objects: {ex}"),
            () => reset.Set());

            reset.Wait();
            stopwatch.Stop();
            _outputHelper.WriteLine($"Cleaned up {0} objects from Minio in {1}s...", count, stopwatch.Elapsed.TotalSeconds);
        }
    }
}
