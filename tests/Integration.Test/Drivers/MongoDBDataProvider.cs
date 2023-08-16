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

using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using MongoDB.Driver;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Drivers
{
    public class MongoDBDataProvider : IDatabaseDataProvider
    {
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly string _databaseName;
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<InferenceRequest> _infereRequestCollection;
        private readonly IMongoCollection<Payload> _payloadCollection;
        private readonly IMongoCollection<StorageMetadataWrapper> _storageMetadataWrapperCollection;
        private readonly IMongoCollection<SourceApplicationEntity> _sourceApplicationEntityCollection;
        private readonly IMongoCollection<DestinationApplicationEntity> _destinationApplicationEntityCollection;
        private readonly IMongoCollection<MonaiApplicationEntity> _monaiApplicationEntityCollection;
        private readonly IMongoCollection<VirtualApplicationEntity> _virtualApplicationEntityCollection;

        public MongoDBDataProvider(ISpecFlowOutputHelper outputHelper, Configurations configuration, string connectionString, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or whitespace.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException($"'{nameof(databaseName)}' cannot be null or whitespace.", nameof(databaseName));
            }

            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _databaseName = databaseName;

            _outputHelper.WriteLine($"Connecting to MongoDB at {connectionString}");
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);
            _infereRequestCollection = _database.GetCollection<InferenceRequest>(nameof(InferenceRequest));
            _payloadCollection = _database.GetCollection<Payload>(nameof(Payload));
            _storageMetadataWrapperCollection = _database.GetCollection<StorageMetadataWrapper>(nameof(StorageMetadataWrapper));
            _sourceApplicationEntityCollection = _database.GetCollection<SourceApplicationEntity>(nameof(SourceApplicationEntity));
            _destinationApplicationEntityCollection = _database.GetCollection<DestinationApplicationEntity>(nameof(DestinationApplicationEntity));
            _monaiApplicationEntityCollection = _database.GetCollection<MonaiApplicationEntity>(nameof(MonaiApplicationEntity));
            _virtualApplicationEntityCollection = _database.GetCollection<VirtualApplicationEntity>(nameof(VirtualApplicationEntity));
        }

        public void ClearAllData()
        {
            _outputHelper.WriteLine("Removing data from the database.");
            DumpClear(_infereRequestCollection);
            DumpClear(_payloadCollection);
            DumpClear(_storageMetadataWrapperCollection);
            DumpClear(_sourceApplicationEntityCollection);
            DumpClear(_destinationApplicationEntityCollection);
            DumpClear(_monaiApplicationEntityCollection);
            DumpClear(_virtualApplicationEntityCollection);
            _outputHelper.WriteLine("All data removed from the database.");
        }

        private void DumpClear<T>(IMongoCollection<T> collection)
        {
            _outputHelper.WriteLine($"==={collection.CollectionNamespace.FullName}===");
            foreach (var item in collection.AsQueryable())
            {
                _outputHelper.WriteLine(item.ToString());
            }

            collection.DeleteMany("{ }");

            if (collection.Find("{ }").CountDocuments() > 0)
            {
                throw new Exception("Failed to delete documents");
            }
            _outputHelper.WriteLine($"Data removed from the collection {collection.CollectionNamespace.FullName}.");
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

            await _infereRequestCollection.InsertOneAsync(request).ConfigureAwait(false);
            _outputHelper.WriteLine($"Injected ACR request {request.TransactionId}");
            Console.WriteLine($"Injected ACR request {request.TransactionId}");
            return request.TransactionId;
        }
    }
}
