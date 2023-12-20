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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database.EntityFramework;
using Moq;
using Xunit;
using MongoDbTypes = Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database.MongoDb;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Test.Database
{
    public class DatabaseRegistrarTest
    {
        [Fact]
        public void GivenEntityFrameworkDatabaseType_WhenConfigureIsCalled_AddsDependencies()
        {
            var serviceDescriptors = new List<ServiceDescriptor>();
            var serviceCollection = new Mock<IServiceCollection>();
            serviceCollection.Setup(p => p.Add(It.IsAny<ServiceDescriptor>()));
            serviceCollection.Setup(p => p.GetEnumerator()).Returns(serviceDescriptors.GetEnumerator());

            var registrar = new DatabaseRegistrar();
            var configInMemory = new List<KeyValuePair<string, string>> {
                new("top:InformaticsGatewayDatabase","DataSource=file::memory:?cache=shared"),
            };

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(configInMemory).Build();

            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var returnedServiceCollection = registrar.Configure(
                serviceCollection.Object,
                DatabaseType.EntityFramework,
                configuration.GetSection("top"),
                configuration.GetSection("top"), loggerFactoryMock.Object);

            Assert.Same(serviceCollection.Object, returnedServiceCollection);

            serviceCollection.Verify(p => p.Add(It.IsAny<ServiceDescriptor>()), Times.Exactly(6));
            serviceCollection.Verify(p => p.Add(It.Is<ServiceDescriptor>(p => p.ServiceType == typeof(RemoteAppExecutionDbContext))), Times.Once());
            serviceCollection.Verify(p => p.Add(It.Is<ServiceDescriptor>(p => p.ServiceType == typeof(IDatabaseMigrationManagerForPlugIns) && p.ImplementationType == typeof(MigrationManager))), Times.Once());
            serviceCollection.Verify(p => p.Add(It.Is<ServiceDescriptor>(p => p.ServiceType == typeof(IRemoteAppExecutionRepository) && p.ImplementationType == typeof(RemoteAppExecutionRepository))), Times.Once());
        }

        [Fact]
        public void GivenMongoDatabaseType_WhenConfigureIsCalled_AddsDependencies()
        {
            var serviceDescriptors = new List<ServiceDescriptor>();
            var serviceCollection = new Mock<IServiceCollection>();
            serviceCollection.Setup(p => p.Add(It.IsAny<ServiceDescriptor>()));
            serviceCollection.Setup(p => p.GetEnumerator()).Returns(serviceDescriptors.GetEnumerator());

            var registrar = new DatabaseRegistrar();
            var configInMemory = new List<KeyValuePair<string, string>> {
                new("top:InformaticsGatewayDatabase","DataSource=file::memory:?cache=shared"),
            };

            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(configInMemory).Build();
            var returnedServiceCollection = registrar.Configure(
                serviceCollection.Object,
                DatabaseType.MongoDb,
                configuration.GetSection("top"),
                configuration.GetSection("top"),
                loggerFactoryMock.Object);

            Assert.Same(serviceCollection.Object, returnedServiceCollection);

            serviceCollection.Verify(p => p.Add(It.Is<ServiceDescriptor>(p => p.ServiceType == typeof(IDatabaseMigrationManagerForPlugIns) && p.ImplementationType == typeof(MongoDbTypes.MigrationManager))), Times.Once());
            serviceCollection.Verify(p => p.Add(It.Is<ServiceDescriptor>(p => p.ServiceType == typeof(IRemoteAppExecutionRepository) && p.ImplementationType == typeof(MongoDbTypes.RemoteAppExecutionRepository))), Times.Once());
        }
    }
}
