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
using Microsoft.EntityFrameworkCore;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database.EntityFramework;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Test.Database.EntityFramework
{
    [CollectionDefinition("SqliteDatabase")]
    public class SqliteDatabaseCollection : ICollectionFixture<SqliteDatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class SqliteDatabaseFixture
    {
        public RemoteAppExecutionDbContext DatabaseContext { get; set; }
        public IList<RemoteAppExecution> RemoteAppExecutions { get; init; }

        public SqliteDatabaseFixture()
        {
            var options = new DbContextOptionsBuilder<RemoteAppExecutionDbContext>()
                .UseSqlite("DataSource=file::memory:?cache=shared")
                .Options;
            DatabaseContext = new RemoteAppExecutionDbContext(options);
            DatabaseContext.Database.EnsureCreated();

            RemoteAppExecutions = new List<RemoteAppExecution>();
        }

        public void Dispose()
        {
            DatabaseContext.Dispose();
        }

        internal void InitDatabaseWithRemoteAppExecutions()
        {
            var set = DatabaseContext.Set<RemoteAppExecution>();
            set.RemoveRange(set.ToList());
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

                set.Add(record);
                RemoteAppExecutions.Add(record);
            }

            DatabaseContext.SaveChanges();
        }
    }
}
