// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class MonaiAeTitleControllerTest
    {
        private MonaiAeTitleController _controller;
        private Mock<IServiceProvider> _serviceProvider;
        private Mock<ProblemDetailsFactory> _problemDetailsFactory;
        private Mock<ILogger<MonaiAeTitleController>> _logger;
        private Mock<ILogger<ConfigurationValidator>> _validationLogger;
        private Mock<IMonaiAeChangedNotificationService> _aeChangedNotificationService;
        private IOptions<InformaticsGatewayConfiguration> _configuration;
        private ConfigurationValidator _configurationValidator;
        private Mock<IInformaticsGatewayRepository<MonaiApplicationEntity>> _repository;

        public MonaiAeTitleControllerTest()
        {
            _serviceProvider = new Mock<IServiceProvider>();
            _logger = new Mock<ILogger<MonaiAeTitleController>>();
            _validationLogger = new Mock<ILogger<ConfigurationValidator>>();
            _aeChangedNotificationService = new Mock<IMonaiAeChangedNotificationService>();
            _configurationValidator = new ConfigurationValidator(_validationLogger.Object);
            _configuration = Options.Create(new InformaticsGatewayConfiguration());

            _problemDetailsFactory = new Mock<ProblemDetailsFactory>();
            _problemDetailsFactory.Setup(_ => _.CreateProblemDetails(
                    It.IsAny<HttpContext>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())
                )
                .Returns((HttpContext httpContext, int? statusCode, string title, string type, string detail, string instance) =>
                {
                    return new ProblemDetails
                    {
                        Status = statusCode,
                        Title = title,
                        Type = type,
                        Detail = detail,
                        Instance = instance
                    };
                });

            _repository = new Mock<IInformaticsGatewayRepository<MonaiApplicationEntity>>();

            _controller = new MonaiAeTitleController(
                 _serviceProvider.Object,
                 _logger.Object,
                 _configurationValidator,
                 _configuration,
                 _aeChangedNotificationService.Object,
                 _repository.Object)
            {
                ProblemDetailsFactory = _problemDetailsFactory.Object
            };
        }

        #region Get

        [RetryFact(DisplayName = "Get - Shall return available MONAI AETs")]
        public async void Get_ShallReturnAllMonaiAets()
        {
            var data = new List<MonaiApplicationEntity>();
            for (int i = 1; i <= 5; i++)
            {
                data.Add(new MonaiApplicationEntity()
                {
                    AeTitle = $"AET{i}",
                    Name = $"AET{i}",
                    Applications = new List<string> { "A", "B" }
                });
            }

            _repository.Setup(p => p.ToListAsync()).Returns(Task.FromResult(data));

            var result = await _controller.Get();
            Assert.Equal(data.Count, result.Value.Count());
            _repository.Verify(p => p.ToListAsync(), Times.Once());
        }

        [RetryFact(DisplayName = "Get - Shall return problem on failure")]
        public async void Get_ShallReturnProblemOnFailure()
        {
            _repository.Setup(p => p.ToListAsync()).Throws(new Exception("error"));

            var result = await _controller.Get();
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error querying database.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
        }

        #endregion Get

        #region GetAeTitle

        [RetryFact(DisplayName = "GetAeTitle - Shall return matching object")]
        public async void GetAeTitle_ReturnsAMatch()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(
                Task.FromResult(
                new MonaiApplicationEntity
                {
                    AeTitle = value,
                    Name = value,
                    Applications = new List<string> { "A", "B" }
                }));

            var result = await _controller.GetAeTitle(value);
            Assert.NotNull(result.Value);
            Assert.Equal(value, result.Value.AeTitle);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [RetryFact(DisplayName = "GetAeTitle - Shall return 404 if not found")]
        public async void GetAeTitle_Returns404IfNotFound()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(default(MonaiApplicationEntity)));

            var result = await _controller.GetAeTitle(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [RetryFact(DisplayName = "GetAeTitle - Shall return problem on failure")]
        public async void GetAeTitle_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Throws(new Exception("error"));

            var result = await _controller.GetAeTitle(value);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error querying MONAI Application Entity.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        #endregion GetAeTitle

        #region Create

        [RetryTheory(DisplayName = "Create - Shall return BadRequest when validation fails")]
        [InlineData("AeTitleIsTooooooLooooong", "'AeTitleIsTooooooLooooong' is not a valid AE Title (source: MonaiApplicationEntity).")]
        [InlineData("AET1", "MONAI AE Title AET1 already exists.")]
        public async void Create_ShallReturnBadRequestOnValidationFailure(string aeTitle, string errorMessage)
        {
            var data = new List<MonaiApplicationEntity>();
            for (int i = 1; i <= 3; i++)
            {
                data.Add(new MonaiApplicationEntity()
                {
                    AeTitle = $"AET{i}",
                    Name = $"AET{i}",
                    Applications = new List<string> { "A", "B" }
                });
            }
            _repository.Setup(p => p.AsQueryable()).Returns(data.AsQueryable());

            var monaiAeTitle = new MonaiApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                Applications = new List<string> { "A", "B" }
            };

            var result = await _controller.Create(monaiAeTitle);

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Validation error.", problem.Title);
            Assert.Equal(errorMessage, problem.Detail);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        }

        [RetryFact(DisplayName = "Create - Shall return problem if failed to add")]
        public async void Create_ShallReturnBadRequestOnAddFailure()
        {
            var aeTitle = "AET";
            var monaiAeTitle = new MonaiApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                Applications = new List<string> { "A", "B" }
            };

            _repository.Setup(p => p.AddAsync(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.Create(monaiAeTitle);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error adding new MONAI Application Entity.", problem.Title);
            Assert.Equal($"error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);

            _repository.Verify(p => p.AddAsync(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(DisplayName = "Create - Shall return CreatedAtAction")]
        public async void Create_ShallReturnCreatedAtAction()
        {
            var aeTitle = "AET";
            var monaiAeTitle = new MonaiApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                Applications = new List<string> { "A", "B" }
            };

            _aeChangedNotificationService.Setup(p => p.Notify(It.IsAny<MonaiApplicationentityChangedEvent>()));
            _repository.Setup(p => p.AddAsync(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()));
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()));

            var result = await _controller.Create(monaiAeTitle);

            Assert.IsType<CreatedAtActionResult>(result.Result);

            _aeChangedNotificationService.Verify(p => p.Notify(It.Is<MonaiApplicationentityChangedEvent>(x => x.ApplicationEntity == monaiAeTitle)), Times.Once());
            _repository.Verify(p => p.AddAsync(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion Create

        #region Delete

        [RetryFact(DisplayName = "Delete - Shall return deleted object")]
        public async void Delete_ReturnsDeleted()
        {
            var value = "AET";
            var entity = new MonaiApplicationEntity
            {
                AeTitle = value,
                Name = value
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(entity));

            _repository.Setup(p => p.Remove(It.IsAny<MonaiApplicationEntity>()));
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()));

            var result = await _controller.Delete(value);
            Assert.NotNull(result.Value);
            Assert.Equal(value, result.Value.AeTitle);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
            _repository.Verify(p => p.Remove(entity), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(DisplayName = "Delete - Shall return 404 if not found")]
        public async void Delete_Returns404IfNotFound()
        {
            var value = "AET";
            var entity = new MonaiApplicationEntity
            {
                AeTitle = value,
                Name = value
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(default(MonaiApplicationEntity)));

            var result = await _controller.Delete(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [RetryFact(DisplayName = "Delete - Shall return problem on failure")]
        public async void Delete_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            var entity = new MonaiApplicationEntity
            {
                AeTitle = value,
                Name = value
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.Remove(It.IsAny<MonaiApplicationEntity>())).Throws(new Exception("error"));

            var result = await _controller.Delete(value);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error deleting MONAI Application Entity.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        #endregion Delete
    }
}