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

            Assert.Collection(result,
                items => items.ServiceName.Equals("DataRetrievalService"),
                items => items.ServiceName.Equals("ScpService"),
                items => items.ServiceName.Equals("SpaceReclaimerService"),
                items => items.ServiceName.Equals("DicomWebExportService"),
                items => items.ServiceName.Equals("ScuExportService"),
                items => items.ServiceName.Equals("PayloadNotificationService"));
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
