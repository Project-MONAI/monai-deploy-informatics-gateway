using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Repositories
{
    public class MonaiServiceLocatorTest
    {
        private readonly Mock<IServiceProvider> _serviceProvider;

        public MonaiServiceLocatorTest()
        {
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider.Setup(p => p.GetService(It.IsAny<Type>())).Returns((Type type) =>
            {
                var mock = new Mock<IMonaiService>();
                mock.SetupGet(p => p.Status).Returns(Api.Rest.ServiceStatus.Running);
                mock.SetupGet(p => p.ServiceName).Returns(type.Name);
                return mock.Object;
            });
        }

        [Fact(DisplayName = "GetMonaiServices")]
        public void GetMonaiServices()
        {
            var serviceLocator = new MonaiServiceLocator(_serviceProvider.Object);
            var result = serviceLocator.GetMonaiServices();

            Assert.Equal(6, result.Count());
            Assert.Equal(1, result.Count(p => p.ServiceName == "DataRetrievalService"));
            Assert.Equal(1, result.Count(p => p.ServiceName == "WorkloadManagerNotificationService"));
            Assert.Equal(1, result.Count(p => p.ServiceName == "ScpService"));
            Assert.Equal(1, result.Count(p => p.ServiceName == "SpaceReclaimerService"));
            Assert.Equal(1, result.Count(p => p.ServiceName == "DicomWebExportService"));
            Assert.Equal(1, result.Count(p => p.ServiceName == "ScuExportService"));
        }

        [Fact(DisplayName = "GetServiceStatus")]
        public void GetServiceStatus()
        {
            var serviceLocator = new MonaiServiceLocator(_serviceProvider.Object);
            var result = serviceLocator.GetServiceStatus();

            Assert.Equal(6, result.Count());
            foreach (var svc in result.Keys)
            {
                Assert.Equal(ServiceStatus.Running, result[svc]);
            }
        }
    }
}
