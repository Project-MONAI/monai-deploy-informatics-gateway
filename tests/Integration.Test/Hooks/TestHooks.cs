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
using BoDi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.Messaging.RabbitMQ;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Hooks
{
    [Binding]
    public sealed class TestHooks
    {
        private static IHost s_informaticsGatewayHost;
        private static IOptions<InformaticsGatewayConfiguration> s_options;
        private static RabbitMQConnectionFactory s_rabbitMqConnectionFactory;
        private static RabbitMQMessagePublisherService s_rabbitMqPublisher;
        private static RabbitMqConsumer s_rabbitMqConsumer_WorkflowRequest;
        private static RabbitMqConsumer s_rabbitMqConsumer_ExportComplete;
        private static EfDataProvider s_database;
        private static DicomScp s_dicomServer;
        private static DataProvider s_dataProvider;
        private static Assertions s_assertions;
        private static FhirDataClient s_fhirSink;
        private static DicomWebDataClient s_dicomWebSink;
        private static MinioDataClient s_minioSink;
        private static Hl7DataClient s_hl7Sink;
        private static DicomCEchoDataClient s_echoscu;
        private static DicomCStoreDataClient s_storescu;
        private static InformaticsGatewayClient s_informaticsGatewayClient;

        private readonly IObjectContainer _objectContainer;

        public TestHooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        /// <summary>
        /// Runs before all tests to create static implementions of Rabbit and Mongo clients as well as starting the WorkflowManager using WebApplicationFactory.
        /// </summary>
        [BeforeTestRun(Order = 0)]
        public static void Init(ISpecFlowOutputHelper outputHelper)
        {
            SetupInformaticsGateway();
            using var scope = s_informaticsGatewayHost.Services.CreateScope();
            s_options = scope.ServiceProvider.GetRequiredService<IOptions<InformaticsGatewayConfiguration>>();
            Configurations.Initialize(outputHelper);
            SetupRabbitMq(outputHelper, scope);

            s_database = GetDatabase(scope.ServiceProvider, outputHelper);
            s_dicomServer = new DicomScp(outputHelper);
            s_dataProvider = new DataProvider(Configurations.Instance, outputHelper);
            s_assertions = new Assertions(Configurations.Instance, s_options.Value, outputHelper);
            s_fhirSink = new FhirDataClient(Configurations.Instance, s_options.Value, outputHelper);
            s_dicomWebSink = new DicomWebDataClient(Configurations.Instance, s_options.Value, outputHelper);
            s_minioSink = new MinioDataClient(Configurations.Instance, s_options.Value, outputHelper);
            s_hl7Sink = new Hl7DataClient(Configurations.Instance, s_options.Value, outputHelper);
            s_echoscu = new DicomCEchoDataClient(Configurations.Instance, s_options.Value, outputHelper);
            s_storescu = new DicomCStoreDataClient(Configurations.Instance, s_options.Value, outputHelper);
            s_informaticsGatewayClient = new InformaticsGatewayClient(HttpClientFactory.Create(), scope.ServiceProvider.GetRequiredService<ILogger<InformaticsGatewayClient>>());
            s_informaticsGatewayClient.ConfigureServiceUris(new Uri(Configurations.Instance.InformaticsGatewayOptions.ApiEndpoint));

            var serviceLocator = scope.ServiceProvider.GetRequiredService<IMonaiServiceLocator>();
            s_informaticsGatewayHost.Start();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            do
            {
                var statuses = serviceLocator.GetServiceStatus();
                if (statuses.Values.All(p => p == Api.Rest.ServiceStatus.Running))
                {
                    break;
                }

                if (stopwatch.Elapsed > TimeSpan.FromSeconds(30))
                {
                    throw new ApplicationException("Timeout waiting for all services to be ready.");
                }
            } while (true);
        }

        private static void SetupRabbitMq(ISpecFlowOutputHelper outputHelper, IServiceScope scope)
        {
            RabbitConnectionFactory.DeleteAllQueues(s_options.Value);

            s_rabbitMqConnectionFactory = new RabbitMQConnectionFactory(
                scope.ServiceProvider.GetRequiredService<ILogger<RabbitMQConnectionFactory>>());

            s_rabbitMqPublisher = new RabbitMQMessagePublisherService(
                Options.Create(s_options.Value.Messaging),
                scope.ServiceProvider.GetRequiredService<ILogger<RabbitMQMessagePublisherService>>(),
                s_rabbitMqConnectionFactory);

            var rabbitMqSubscriber_WorkflowRequest = new RabbitMQMessageSubscriberService(
                Options.Create(s_options.Value.Messaging),
                scope.ServiceProvider.GetRequiredService<ILogger<RabbitMQMessageSubscriberService>>(),
                s_rabbitMqConnectionFactory);

            s_rabbitMqConsumer_WorkflowRequest = new RabbitMqConsumer(rabbitMqSubscriber_WorkflowRequest, s_options.Value.Messaging.Topics.WorkflowRequest, outputHelper);

            var rabbitMqSubscriber_ExportComplete = new RabbitMQMessageSubscriberService(
                Options.Create(s_options.Value.Messaging),
                scope.ServiceProvider.GetRequiredService<ILogger<RabbitMQMessageSubscriberService>>(),
                s_rabbitMqConnectionFactory);

            s_rabbitMqConsumer_ExportComplete = new RabbitMqConsumer(rabbitMqSubscriber_ExportComplete, s_options.Value.Messaging.Topics.ExportComplete, outputHelper);
        }

        private static EfDataProvider GetDatabase(IServiceProvider serviceProvider, ISpecFlowOutputHelper outputHelper)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
            var connectionString = config.GetSection("ConnectionStrings:InformaticsGatewayDatabase").Value;

            var builder = new DbContextOptionsBuilder<InformaticsGatewayContext>();
            builder.UseSqlite(connectionString);
            var dbContext = new InformaticsGatewayContext(builder.Options);
            return new EfDataProvider(outputHelper, Configurations.Instance, dbContext);
        }

        [BeforeScenario(Order = 0)]
        public void SetUp(ScenarioContext scenarioContext, ISpecFlowOutputHelper outputHelper)
        {
            _objectContainer.RegisterInstanceAs(Configurations.Instance);
            _objectContainer.RegisterInstanceAs<IDatabaseDataProvider>(s_database, "Database");
            _objectContainer.RegisterInstanceAs(s_options.Value, "InformaticsGatewayConfiguration");
            _objectContainer.RegisterInstanceAs(s_dicomServer, "DicomScp");
            _objectContainer.RegisterInstanceAs(s_rabbitMqPublisher, "MessagingPublisher");
            _objectContainer.RegisterInstanceAs(s_rabbitMqConsumer_WorkflowRequest, "WorkflowRequestSubscriber");
            _objectContainer.RegisterInstanceAs(s_rabbitMqConsumer_ExportComplete, "ExportCompleteSubscriber");
            _objectContainer.RegisterInstanceAs(s_dataProvider, "DataProvider");
            _objectContainer.RegisterInstanceAs(s_assertions, "Assertions");
            _objectContainer.RegisterInstanceAs<IDataClient>(s_storescu, "StoreSCU");
            _objectContainer.RegisterInstanceAs<IDataClient>(s_echoscu, "EchoSCU");
            _objectContainer.RegisterInstanceAs<IDataClient>(s_fhirSink, "FhirClient");
            _objectContainer.RegisterInstanceAs<IDataClient>(s_dicomWebSink, "DicomWebClient");
            _objectContainer.RegisterInstanceAs<IDataClient>(s_minioSink, "MinioClient");
            _objectContainer.RegisterInstanceAs<IDataClient>(s_hl7Sink, "HL7Client");
            _objectContainer.RegisterInstanceAs(s_informaticsGatewayClient, "InformaticsGatewayClient");
        }

        [AfterTestRun(Order = 1)]
        public static void Shtudown()
        {
            s_informaticsGatewayHost.StopAsync().Wait();
            s_dicomServer.Dispose();
        }

        [AfterTestRun(Order = 0)]
        [AfterScenario]
        public static void ClearTestData()
        {
            RabbitConnectionFactory.PurgeAllQueues(s_options.Value.Messaging);
        }

        private static void SetupInformaticsGateway()
        {
            s_informaticsGatewayHost = Program.CreateHostBuilder(Array.Empty<string>()).Build();
            s_informaticsGatewayHost.MigrateDatabase();
        }
    }
}
