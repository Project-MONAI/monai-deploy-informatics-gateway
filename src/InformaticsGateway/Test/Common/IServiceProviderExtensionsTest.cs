// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using System;
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
            var typeString = this.GetType().AssemblyQualifiedName;
            Assert.Throws<ConfigurationException>(() => _serviceProvider.Object.LocateService<object>(_logger.Object, typeString));

            _logger.VerifyLogging($"Instance of '{typeString}' cannot be found.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "LocateService shall return an instance of specified type")]
        public void LocateService_ShallReturnInstance()
        {
            _serviceProvider.Setup(p => p.GetService(It.IsAny<Type>())).Returns(this);

            var typeString = this.GetType().AssemblyQualifiedName;
            var instance = _serviceProvider.Object.LocateService<object>(_logger.Object, typeString);

            Assert.Equal(this, instance);
        }
    }
}
