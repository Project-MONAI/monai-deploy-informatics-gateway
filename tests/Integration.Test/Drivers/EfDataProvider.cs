/*
 * Copyright 2022-2023 MONAI Consortium
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
using Microsoft.EntityFrameworkCore;
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
        private readonly InformaticsGatewayContext _dbContext;

        public EfDataProvider(ISpecFlowOutputHelper outputHelper, Configurations configuration, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or whitespace.", nameof(connectionString));
            }

            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            connectionString = ConvertToFullPath(connectionString);
            _outputHelper.WriteLine($"Connecting to EF based database using {connectionString}");
            var builder = new DbContextOptionsBuilder<InformaticsGatewayContext>();
            builder.UseSqlite(connectionString);
            _dbContext = new InformaticsGatewayContext(builder.Options);
        }

        private string ConvertToFullPath(string connectionString)
        {
            Guard.Against.NullOrWhiteSpace(connectionString, nameof(connectionString));

            string absolute = Path.GetFullPath("./");
            return connectionString.Replace("=./", $"={absolute}");
        }

        public void ClearAllData()
        {
            _outputHelper.WriteLine("Removing data from the database.");
            _dbContext.Database.EnsureCreated();
            DumpAndClear("DestinationApplicationEntities", _dbContext.DestinationApplicationEntities.ToList());
            DumpAndClear("SourceApplicationEntities", _dbContext.SourceApplicationEntities.ToList());
            DumpAndClear("MonaiApplicationEntities", _dbContext.MonaiApplicationEntities.ToList());
            DumpAndClear("VirtualApplicationEntities", _dbContext.VirtualApplicationEntities.ToList());
            DumpAndClear("Payloads", _dbContext.Payloads.ToList());
            DumpAndClear("InferenceRequests", _dbContext.InferenceRequests.ToList());
            DumpAndClear("StorageMetadataWrapperEntities", _dbContext.StorageMetadataWrapperEntities.ToList());
            _dbContext.SaveChanges();
            _outputHelper.WriteLine("All data removed from the database.");
        }

        private void DumpAndClear<T>(string name, List<T> items) where T : class
        {
            _outputHelper.WriteLine($"==={name}===");
            foreach (var item in items)
            {
                _outputHelper.WriteLine(item.ToString());
            }
            _dbContext.Set<T>().RemoveRange(items);
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
            Console.WriteLine($"Injected ACR request {request.TransactionId}");
            return request.TransactionId;
        }
    }
}
