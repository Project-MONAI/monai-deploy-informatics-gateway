/*
 * Copyright 2023 MONAI Consortium
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

using FellowOakDicom;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database.MongoDb;
using MongoDB.Driver;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Test.Database.MongoDb
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
        public IList<RemoteAppExecution> RemoteAppExecutions { get; init; }

        public MongoDatabaseFixture()
        {
            Client = new MongoClient("mongodb://root:rootpassword@localhost:27017");
            Options = Microsoft.Extensions.Options.Options.Create(new DatabaseOptions { DatabaseName = $"IGTest" });
            Database = Client.GetDatabase(Options.Value.DatabaseName);

            var migration = new MigrationManager();
            migration.Migrate(null!);

            RemoteAppExecutions = new List<RemoteAppExecution>();
        }

        internal void InitDatabaseWithRemoteAppExecutions()
        {
            var collection = Database.GetCollection<RemoteAppExecution>(nameof(RemoteAppExecution));
            Clear(collection);
            RemoteAppExecutions.Clear();
            for (var i = 0; i < 5; i++)
            {
                var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
                var record = new RemoteAppExecution
                {
                    WorkflowInstanceId = Guid.NewGuid().ToString(),
                    CorrelationId = Guid.NewGuid().ToString(),
                    ExportTaskId = Guid.NewGuid().ToString(),
                    Id = Guid.NewGuid(),
                    RequestTime = DateTimeOffset.UtcNow,
                    StudyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                    SeriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                    SopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                };

                record.OriginalValues.Add(DicomTag.StudyInstanceUID.ToString(), studyInstanceUid);
                record.OriginalValues.Add(DicomTag.SeriesInstanceUID.ToString(), DicomUIDGenerator.GenerateDerivedFromUUID().UID);
                record.OriginalValues.Add(DicomTag.SOPInstanceUID.ToString(), DicomUIDGenerator.GenerateDerivedFromUUID().UID);
                record.OriginalValues.Add(DicomTag.PatientID.ToString(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16));
                record.OriginalValues.Add(DicomTag.AccessionNumber.ToString(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16));
                record.OriginalValues.Add(DicomTag.StudyDescription.ToString(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16));

                collection.InsertOne(record);
                RemoteAppExecutions.Add(record);
            }
        }

        public static void Clear<T>(IMongoCollection<T> collection) where T : class
        {
            collection.DeleteMany(Builders<T>.Filter.Empty);
        }
    }
}
