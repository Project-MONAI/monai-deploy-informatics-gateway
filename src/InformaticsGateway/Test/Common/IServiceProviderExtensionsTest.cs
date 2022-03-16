// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Common
{
    public class IServiceProviderExtensionsTest
    {
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly Mock<ILogger<Program>> _logger;

        public IServiceProviderExtensionsTest()
        {
            _serviceProvider = new Mock<IServiceProvider>();
            _logger = new Mock<ILogger<Program>>();
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact(DisplayName = "LocateService shall throw when type is unknown")]
        public void LocateService_ShallThrowWhenTypeIsUnknown()
        {
            var typeString = "TestTest";
            Assert.Throws<ConfigurationException>(() => _serviceProvider.Object.LocateService<object>(_logger.Object, typeString));

            _logger.VerifyLogging($"Type '{typeString}' cannot be found.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "LocateService shall throw when instance of type cannot be found")]
        public void LocateService_ShallThrowWhenInstanceOfTypeCannotBeFound()
        {
            var typeString = GetType().AssemblyQualifiedName;
            Assert.Throws<ConfigurationException>(() => _serviceProvider.Object.LocateService<object>(_logger.Object, typeString));

            _logger.VerifyLogging($"Instance of '{typeString}' cannot be found.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "LocateService shall return an instance of specified type")]
        public void LocateService_ShallReturnInstance()
        {
            _serviceProvider.Setup(p => p.GetService(It.IsAny<Type>())).Returns(this);

            var typeString = GetType().AssemblyQualifiedName;
            var instance = _serviceProvider.Object.LocateService<object>(_logger.Object, typeString);

            Assert.Equal(this, instance);
        }
    }
}
