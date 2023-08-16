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

using FellowOakDicom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Test;
using Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories;
using MongoDB.Driver;
using Moq;
namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Integration.Test
{
    [Collection("MongoDatabase")]
    public class RemoteAppRepositoryTest
    {
        private readonly MongoDatabaseFixture _databaseFixture;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<RemoteAppExecutionRepository>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;

        public RemoteAppRepositoryTest(MongoDatabaseFixture databaseFixture)
        {

            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<RemoteAppExecutionRepository>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => databaseFixture.Client);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Database.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public async Task GivenAexecution_WhenAddingToDatabase_ExpectItToBeSaved()
        {
            var outgoingUid = Guid.NewGuid().ToString();
            var dataset = new DicomDataset();
            var date = new DateTime(
                DateTime.Now.Year,
                DateTime.Now.Month,
                DateTime.Now.Day,
                DateTime.Now.Hour,
                DateTime.Now.Minute,
                DateTime.Now.Second).ToUniversalTime();

            var execution = new RemoteAppExecution
            {
                CorrelationId = Guid.NewGuid().ToString(),
                ExportTaskId = "ExportTaskId",
                WorkflowInstanceId = "WorkflowInstanceId",
                OutgoingUid = outgoingUid,
                StudyUid = Guid.NewGuid().ToString(),
                RequestTime = date,
                OriginalValues = new Dictionary<string, string> {
                    { DicomTag.StudyInstanceUID.ToString(), "StudyInstanceUID" },
                    { DicomTag.SeriesInstanceUID.ToString(), "SeriesInstanceUID" }
                },
                ProxyValues = new Dictionary<string, string> {
                    { DicomTag.StudyInstanceUID.ToString(), "StudyInstanceUID" },
                    { DicomTag.SeriesInstanceUID.ToString(), "SeriesInstanceUID" }
                }
            };


            var store = new RemoteAppExecutionRepository(_serviceScopeFactory.Object, _logger.Object, _options, _databaseFixture.Options);
            await store.AddAsync(execution).ConfigureAwait(false);

            var collection = _databaseFixture.Database.GetCollection<RemoteAppExecution>(nameof(RemoteAppExecution));

            var actual = await store.GetAsync(execution.OutgoingUid).ConfigureAwait(false);

            Task.Delay(1000).Wait();
            Assert.NotNull(actual);
            Assert.Equal(execution.CorrelationId, actual!.CorrelationId);
            Assert.Equal(execution.ExportTaskId, actual!.ExportTaskId);
            Assert.Equal(execution.WorkflowInstanceId, actual!.WorkflowInstanceId);
            Assert.Equal(execution.OutgoingUid, actual!.OutgoingUid);
            Assert.Equal(execution.CorrelationId, actual!.CorrelationId);
            Assert.Equal(execution.WorkflowInstanceId, actual!.WorkflowInstanceId);
            Assert.Equal(execution.StudyUid, actual!.StudyUid);
            Assert.Equal(execution.RequestTime, actual!.RequestTime);
            Assert.Equal(execution.OriginalValues, actual!.OriginalValues);
            Assert.Equal(execution.ProxyValues, actual!.ProxyValues);
            Assert.Equal(2, actual!.OriginalValues.Count);

            await store.RemoveAsync(execution.OutgoingUid).ConfigureAwait(false);

            actual = await collection.Find(p => p.OutgoingUid == execution.OutgoingUid).FirstOrDefaultAsync().ConfigureAwait(false);
            Assert.Null(actual);
        }
    }
}
