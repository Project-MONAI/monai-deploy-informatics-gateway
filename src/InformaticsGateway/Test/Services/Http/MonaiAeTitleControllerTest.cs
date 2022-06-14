// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class MonaiAeTitleControllerTest
    {
        private readonly MonaiAeTitleController _controller;
        private readonly Mock<ProblemDetailsFactory> _problemDetailsFactory;
        private readonly Mock<ILogger<MonaiAeTitleController>> _logger;
        private readonly Mock<IMonaiAeChangedNotificationService> _aeChangedNotificationService;
        private readonly Mock<IInformaticsGatewayRepository<MonaiApplicationEntity>> _repository;

        public MonaiAeTitleControllerTest()
        {
            _logger = new Mock<ILogger<MonaiAeTitleController>>();
            _aeChangedNotificationService = new Mock<IMonaiAeChangedNotificationService>();

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
                 _logger.Object,
                 _aeChangedNotificationService.Object,
                 _repository.Object)
            {
                ProblemDetailsFactory = _problemDetailsFactory.Object
            };
        }

        #region Get

        [RetryFact(5, 250, DisplayName = "Get - Shall return available MONAI AETs")]
        public async Task Get_ShallReturnAllMonaiAets()
        {
            var data = new List<MonaiApplicationEntity>();
            for (var i = 1; i <= 5; i++)
            {
                data.Add(new MonaiApplicationEntity()
                {
                    AeTitle = $"AET{i}",
                    Name = $"AET{i}",
                    Workflows = new List<string> { "A", "B" }
                });
            }

            _repository.Setup(p => p.ToListAsync()).Returns(Task.FromResult(data));

            var result = await _controller.Get();
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as IEnumerable<MonaiApplicationEntity>;
            Assert.Equal(data.Count, response.Count());
            _repository.Verify(p => p.ToListAsync(), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Get - Shall return problem on failure")]
        public async Task Get_ShallReturnProblemOnFailure()
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

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return matching object")]
        public async Task GetAeTitle_ReturnsAMatch()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(
                Task.FromResult(
                new MonaiApplicationEntity
                {
                    AeTitle = value,
                    Name = value,
                    Workflows = new List<string> { "A", "B" }
                }));

            var result = await _controller.GetAeTitle(value);
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as MonaiApplicationEntity;
            Assert.NotNull(response);
            Assert.Equal(value, response.AeTitle);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return 404 if not found")]
        public async Task GetAeTitle_Returns404IfNotFound()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(default(MonaiApplicationEntity)));

            var result = await _controller.GetAeTitle(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return problem on failure")]
        public async Task GetAeTitle_ShallReturnProblemOnFailure()
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
        [InlineData("AET1", "A MONAI Application Entity with the same name 'AET1' already exists.")]
        public async Task Create_ShallReturnBadRequestOnValidationFailure(string aeTitle, string errorMessage)
        {
            var data = new List<MonaiApplicationEntity>();
            for (var i = 1; i <= 3; i++)
            {
                data.Add(new MonaiApplicationEntity()
                {
                    AeTitle = $"AET{i}",
                    Name = $"AET{i}",
                    Workflows = new List<string> { "A", "B" }
                });
            }
            _repository.Setup(p => p.Any(It.IsAny<Func<MonaiApplicationEntity, bool>>())).Returns(aeTitle == "AET1");

            var monaiAeTitle = new MonaiApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                Workflows = new List<string> { "A", "B" }
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

        [RetryFact(DisplayName = "Create - Shall return BadRequest when both allowed & ignored SOP classes are defined")]
        public async Task Create_ShallReturnBadRequestWhenBothAllowedAndIgnoredSopsAreDefined()
        {
            var data = new List<MonaiApplicationEntity>();
            for (var i = 1; i <= 3; i++)
            {
                data.Add(new MonaiApplicationEntity()
                {
                    AeTitle = $"AET{i}",
                    Name = $"AET{i}",
                    Workflows = new List<string> { "A", "B" }
                });
            }
            _repository.Setup(p => p.Any(It.IsAny<Func<MonaiApplicationEntity, bool>>())).Returns(false);

            var monaiAeTitle = new MonaiApplicationEntity
            {
                Name = "MyAET",
                AeTitle = "MyAET",
                Workflows = new List<string> { "A", "B" },
                IgnoredSopClasses = new List<string> { "A", "B" },
                AllowedSopClasses = new List<string> { "C", "D" },
            };

            var result = await _controller.Create(monaiAeTitle);

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Validation error.", problem.Title);
            Assert.Equal("Cannot specify both allowed and ignored SOP classes at the same time, they are mutually exclusive.", problem.Detail);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        }

        [RetryFact(5, 250, DisplayName = "Create - Shall return problem if failed to add")]
        public async Task Create_ShallReturnBadRequestOnAddFailure()
        {
            var aeTitle = "AET";
            var monaiAeTitle = new MonaiApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                Workflows = new List<string> { "A", "B" }
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

        [RetryFact(5, 250, DisplayName = "Create - Shall return CreatedAtAction")]
        public async Task Create_ShallReturnCreatedAtAction()
        {
            var aeTitle = "AET";
            var monaiAeTitle = new MonaiApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                Workflows = new List<string> { "A", "B" }
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

        [RetryFact(5, 250, DisplayName = "Delete - Shall return deleted object")]
        public async Task Delete_ReturnsDeleted()
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
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as MonaiApplicationEntity;
            Assert.NotNull(response);
            Assert.Equal(value, response.AeTitle);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
            _repository.Verify(p => p.Remove(entity), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Delete - Shall return 404 if not found")]
        public async Task Delete_Returns404IfNotFound()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(default(MonaiApplicationEntity)));

            var result = await _controller.Delete(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Delete - Shall return problem on failure")]
        public async Task Delete_ShallReturnProblemOnFailure()
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
