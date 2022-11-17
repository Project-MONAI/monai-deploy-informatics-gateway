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

using Microsoft.EntityFrameworkCore;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test
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
        public InformaticsGatewayContext DatabaseContext { get; set; }

        public SqliteDatabaseFixture()
        {
            var options = new DbContextOptionsBuilder<InformaticsGatewayContext>()
                .UseSqlite("DataSource=file::memory:?cache=shared")
                .Options;
            DatabaseContext = new InformaticsGatewayContext(options);
            DatabaseContext.Database.EnsureCreated();
        }

        public void InitDatabaseWithDestinationApplicationEntities()
        {
            var aet1 = new DestinationApplicationEntity { AeTitle = "AET1", HostIp = "1.2.3.4", Port = 114, Name = "AET1" };
            var aet2 = new DestinationApplicationEntity { AeTitle = "AET2", HostIp = "1.2.3.4", Port = 114, Name = "AET2" };
            var aet3 = new DestinationApplicationEntity { AeTitle = "AET3", HostIp = "1.2.3.4", Port = 114, Name = "AET3" };
            var aet4 = new DestinationApplicationEntity { AeTitle = "AET4", HostIp = "1.2.3.4", Port = 114, Name = "AET4" };
            var aet5 = new DestinationApplicationEntity { AeTitle = "AET5", HostIp = "1.2.3.4", Port = 114, Name = "AET5" };

            var set = DatabaseContext.Set<DestinationApplicationEntity>();
            set.RemoveRange(set.ToList());
            set.Add(aet1);
            set.Add(aet2);
            set.Add(aet3);
            set.Add(aet4);
            set.Add(aet5);

            DatabaseContext.SaveChanges();
        }

        public void InitDatabaseWithMonaiApplicationEntities()
        {
            var aet1 = new MonaiApplicationEntity { AeTitle = "AET1", Name = "AET1" };
            var aet2 = new MonaiApplicationEntity { AeTitle = "AET2", Name = "AET2" };
            var aet3 = new MonaiApplicationEntity { AeTitle = "AET3", Name = "AET3" };
            var aet4 = new MonaiApplicationEntity { AeTitle = "AET4", Name = "AET4" };
            var aet5 = new MonaiApplicationEntity { AeTitle = "AET5", Name = "AET5" };

            var set = DatabaseContext.Set<MonaiApplicationEntity>();
            set.RemoveRange(set.ToList());
            set.Add(aet1);
            set.Add(aet2);
            set.Add(aet3);
            set.Add(aet4);
            set.Add(aet5);

            DatabaseContext.SaveChanges();
        }

        public void InitDatabaseWithSourceApplicationEntities()
        {
            var aet1 = new SourceApplicationEntity { AeTitle = "AET1", Name = "AET1", HostIp = "1.2.3.4" };
            var aet2 = new SourceApplicationEntity { AeTitle = "AET2", Name = "AET2", HostIp = "1.2.3.4" };
            var aet3 = new SourceApplicationEntity { AeTitle = "AET3", Name = "AET3", HostIp = "1.2.3.4" };
            var aet4 = new SourceApplicationEntity { AeTitle = "AET4", Name = "AET4", HostIp = "1.2.3.4" };
            var aet5 = new SourceApplicationEntity { AeTitle = "AET5", Name = "AET5", HostIp = "1.2.3.4" };

            var set = DatabaseContext.Set<SourceApplicationEntity>();
            set.RemoveRange(set.ToList());
            set.Add(aet1);
            set.Add(aet2);
            set.Add(aet3);
            set.Add(aet4);
            set.Add(aet5);

            DatabaseContext.SaveChanges();
        }

        public void Clear<T>() where T : class
        {
            var set = DatabaseContext.Set<T>();
            set.RemoveRange(set.ToList());
            DatabaseContext.SaveChanges();
        }

        public void Dispose()
        {
            DatabaseContext.Dispose();
        }
    }
}
