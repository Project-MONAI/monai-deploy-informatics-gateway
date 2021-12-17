using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scp
{
    public class PayloadAssemblerTest
    {
        private readonly Mock<ILogger<PayloadAssembler>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;

        public PayloadAssemblerTest()
        {
            _logger = new Mock<ILogger<PayloadAssembler>>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
        }
    }
}
