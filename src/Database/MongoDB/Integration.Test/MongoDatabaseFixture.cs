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

using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.MongoDB;
using MongoDB.Driver;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test
{
    [CollectionDefinition("MongoDatabase")]
    public class MongoDatabaseCollection : ICollectionFixture<MongoDatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class MongoDatabaseFixture
    {
        public IMongoClient Client { get; set; }
        public IMongoDatabase Database { get; set; }
        public IOptions<DatabaseOptions> Options { get; set; }

        public MongoDatabaseFixture()
        {
            Client = new MongoClient("mongodb://root:rootpassword@localhost:27017");
            Options = Microsoft.Extensions.Options.Options.Create(new DatabaseOptions { DatabaseName = $"IGTest" });
            Database = Client.GetDatabase(Options.Value.DatabaseName);

            var migration = new MongoDatabaseMigrationManager();
            migration.Migrate(null!);
        }

        public void InitDatabaseWithDestinationApplicationEntities()
        {
            var collection = Database.GetCollection<DestinationApplicationEntity>(nameof(DestinationApplicationEntity));
            Clear(collection);
            var aet1 = new DestinationApplicationEntity { AeTitle = "AET1", HostIp = "1.2.3.4", Port = 114, Name = "AET1", DateTimeCreated = DateTime.UtcNow };
            var aet2 = new DestinationApplicationEntity { AeTitle = "AET2", HostIp = "1.2.3.4", Port = 114, Name = "AET2", DateTimeCreated = DateTime.UtcNow };
            var aet3 = new DestinationApplicationEntity { AeTitle = "AET3", HostIp = "1.2.3.4", Port = 114, Name = "AET3", DateTimeCreated = DateTime.UtcNow };
            var aet4 = new DestinationApplicationEntity { AeTitle = "AET4", HostIp = "1.2.3.4", Port = 114, Name = "AET4", DateTimeCreated = DateTime.UtcNow };
            var aet5 = new DestinationApplicationEntity { AeTitle = "AET5", HostIp = "1.2.3.4", Port = 114, Name = "AET5", DateTimeCreated = DateTime.UtcNow };

            collection.InsertOne(aet1);
            collection.InsertOne(aet2);
            collection.InsertOne(aet3);
            collection.InsertOne(aet4);
            collection.InsertOne(aet5);
        }

        public void InitDatabaseWithMonaiApplicationEntities()
        {
            var collection = Database.GetCollection<MonaiApplicationEntity>(nameof(MonaiApplicationEntity));
            Clear(collection);
            var aet1 = new MonaiApplicationEntity { AeTitle = "AET1", Name = "AET1", DateTimeCreated = DateTime.UtcNow };
            var aet2 = new MonaiApplicationEntity { AeTitle = "AET2", Name = "AET2", DateTimeCreated = DateTime.UtcNow };
            var aet3 = new MonaiApplicationEntity { AeTitle = "AET3", Name = "AET3", DateTimeCreated = DateTime.UtcNow };
            var aet4 = new MonaiApplicationEntity { AeTitle = "AET4", Name = "AET4", DateTimeCreated = DateTime.UtcNow };
            var aet5 = new MonaiApplicationEntity { AeTitle = "AET5", Name = "AET5", DateTimeCreated = DateTime.UtcNow };

            collection.InsertOne(aet1);
            collection.InsertOne(aet2);
            collection.InsertOne(aet3);
            collection.InsertOne(aet4);
            collection.InsertOne(aet5);
        }

        public void InitDatabaseWithVirtualApplicationEntities()
        {
            var collection = Database.GetCollection<VirtualApplicationEntity>(nameof(VirtualApplicationEntity));
            Clear(collection);
            var aet1 = new VirtualApplicationEntity { VirtualAeTitle = "AET1", Name = "AET1", DateTimeCreated = DateTime.UtcNow };
            var aet2 = new VirtualApplicationEntity { VirtualAeTitle = "AET2", Name = "AET2", DateTimeCreated = DateTime.UtcNow };
            var aet3 = new VirtualApplicationEntity { VirtualAeTitle = "AET3", Name = "AET3", DateTimeCreated = DateTime.UtcNow };
            var aet4 = new VirtualApplicationEntity { VirtualAeTitle = "AET4", Name = "AET4", DateTimeCreated = DateTime.UtcNow };
            var aet5 = new VirtualApplicationEntity { VirtualAeTitle = "AET5", Name = "AET5", DateTimeCreated = DateTime.UtcNow };

            collection.InsertOne(aet1);
            collection.InsertOne(aet2);
            collection.InsertOne(aet3);
            collection.InsertOne(aet4);
            collection.InsertOne(aet5);
        }

        public void InitDatabaseWithSourceApplicationEntities()
        {
            var collection = Database.GetCollection<SourceApplicationEntity>(nameof(SourceApplicationEntity));
            Clear(collection);
            var aet1 = new SourceApplicationEntity { AeTitle = "AET1", Name = "AET1", HostIp = "1.2.3.4", DateTimeCreated = DateTime.UtcNow };
            var aet2 = new SourceApplicationEntity { AeTitle = "AET2", Name = "AET2", HostIp = "1.2.3.4", DateTimeCreated = DateTime.UtcNow };
            var aet3 = new SourceApplicationEntity { AeTitle = "AET3", Name = "AET3", HostIp = "1.2.3.4", DateTimeCreated = DateTime.UtcNow };
            var aet4 = new SourceApplicationEntity { AeTitle = "AET4", Name = "AET4", HostIp = "1.2.3.4", DateTimeCreated = DateTime.UtcNow };
            var aet5 = new SourceApplicationEntity { AeTitle = "AET5", Name = "AET5", HostIp = "1.2.3.4", DateTimeCreated = DateTime.UtcNow };

            collection.InsertOne(aet1);
            collection.InsertOne(aet2);
            collection.InsertOne(aet3);
            collection.InsertOne(aet4);
            collection.InsertOne(aet5);
        }

        public void InitDatabaseWithInferenceRequests()
        {
            var collection = Database.GetCollection<InferenceRequest>(nameof(InferenceRequest));
            Clear(collection);
        }

        internal void InitDatabaseWithDicomAssociationInfoEntries()
        {
            var collection = Database.GetCollection<DicomAssociationInfo>(nameof(DicomAssociationInfo));
            Clear(collection);

            var da1 = new DicomAssociationInfo { CalledAeTitle = Guid.NewGuid().ToString(), CallingAeTitle = Guid.NewGuid().ToString(), CorrelationId = Guid.NewGuid().ToString(), RemoteHost = "host", RemotePort = 123 };
            var da2 = new DicomAssociationInfo { CalledAeTitle = Guid.NewGuid().ToString(), CallingAeTitle = Guid.NewGuid().ToString(), CorrelationId = Guid.NewGuid().ToString(), RemoteHost = "host", RemotePort = 123 };
            var da3 = new DicomAssociationInfo { CalledAeTitle = Guid.NewGuid().ToString(), CallingAeTitle = Guid.NewGuid().ToString(), CorrelationId = Guid.NewGuid().ToString(), RemoteHost = "host", RemotePort = 123 };
            var da4 = new DicomAssociationInfo { CalledAeTitle = Guid.NewGuid().ToString(), CallingAeTitle = Guid.NewGuid().ToString(), CorrelationId = Guid.NewGuid().ToString(), RemoteHost = "host", RemotePort = 123 };
            var da5 = new DicomAssociationInfo { CalledAeTitle = Guid.NewGuid().ToString(), CallingAeTitle = Guid.NewGuid().ToString(), CorrelationId = Guid.NewGuid().ToString(), RemoteHost = "host", RemotePort = 123 };

            collection.InsertOne(da1);
            collection.InsertOne(da2);
            collection.InsertOne(da3);
            collection.InsertOne(da4);
            collection.InsertOne(da5);
        }

        public static void Clear<T>(IMongoCollection<T> collection) where T : class
        {
            collection.DeleteMany(Builders<T>.Filter.Empty);
        }
    }
}
