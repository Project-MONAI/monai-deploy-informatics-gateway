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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database.EntityFramework;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Test.Database.EntityFramework
{
    public class MigrationManagerTest
    {
        private readonly Mock<IHost> _host;
        private readonly RemoteAppExecutionDbContext _dbContext;
        private readonly IServiceProvider _serviceProvider;

        public MigrationManagerTest()
        {
            var options = new DbContextOptionsBuilder<RemoteAppExecutionDbContext>()
                .UseSqlite("DataSource=file:memdbmigration?mode=memory&cache=shared")
                .Options;

            _host = new Mock<IHost>();
            _dbContext = new RemoteAppExecutionDbContext(options);

            var services = new ServiceCollection();
            services.AddScoped(p => _dbContext);

            _serviceProvider = services.BuildServiceProvider();
            _host.Setup(p => p.Services).Returns(_serviceProvider);
        }

        [Fact]
        public void GivenARemoteAppExecutionDbContext_OnMigration_MigratesSuccessfully()
        {
            var mgr = new MigrationManager();
            var result = mgr.Migrate(_host.Object);

            Assert.Same(_host.Object, result);
        }
    }
}
