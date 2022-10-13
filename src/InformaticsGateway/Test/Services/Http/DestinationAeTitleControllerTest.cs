/*
 * Copyright 2021-2022 MONAI Consortium
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
using Monai.Deploy.InformaticsGateway.Services.Scu;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class DestinationAeTitleControllerTest
    {
        private readonly DestinationAeTitleController _controller;
        private readonly Mock<ProblemDetailsFactory> _problemDetailsFactory;
        private readonly Mock<ILogger<DestinationAeTitleController>> _logger;
        private readonly Mock<IScuQueue> _scuQueue;
        private readonly Mock<IInformaticsGatewayRepository<DestinationApplicationEntity>> _repository;

        public DestinationAeTitleControllerTest()
        {
            _logger = new Mock<ILogger<DestinationAeTitleController>>();
            _scuQueue = new Mock<IScuQueue>();

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

            _repository = new Mock<IInformaticsGatewayRepository<DestinationApplicationEntity>>();

            var controllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            _controller = new DestinationAeTitleController(
                 _logger.Object,
                 _repository.Object,
                 _scuQueue.Object)
            {
                ProblemDetailsFactory = _problemDetailsFactory.Object,
                ControllerContext = controllerContext
            };
        }

        #region Get

        [RetryFact(5, 250, DisplayName = "Get - Shall return available destination AETs")]
        public async Task Get_ShallReturnAllDestinationAets()
        {
            var data = new List<DestinationApplicationEntity>();
            for (var i = 1; i <= 5; i++)
            {
                data.Add(new DestinationApplicationEntity()
                {
                    AeTitle = $"AET{i}",
                    Name = $"AET{i}",
                    HostIp = "host",
                    Port = 123
                });
            }

            _repository.Setup(p => p.ToListAsync()).Returns(Task.FromResult(data));

            var result = await _controller.Get();
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as IEnumerable<DestinationApplicationEntity>;
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
                new DestinationApplicationEntity
                {
                    AeTitle = value,
                    Name = value
                }));

            var result = await _controller.GetAeTitle(value);
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as DestinationApplicationEntity;
            Assert.NotNull(response);
            Assert.Equal(value, response.AeTitle);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return 404 if not found")]
        public async Task GetAeTitle_Returns404IfNotFound()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(default(DestinationApplicationEntity)));

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
            Assert.Equal("Error querying DICOM destinations.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        #endregion GetAeTitle

        #region C-Echo

        [RetryFact(5, 250)]
        public async Task GivenAnEmptyString_WhenCEchoIsCalled_Returns404()
        {
            var result = await _controller.CEcho(string.Empty);
            var notFoundResult = result as NotFoundResult;
            Assert.NotNull(notFoundResult);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [RetryFact(5, 250)]
        public async Task GivenADestinationName_WhenCEchoIsCalledAndEntityCannotBeFound_Returns404()
        {
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).ReturnsAsync(default(DestinationApplicationEntity));
            var result = await _controller.CEcho("AET");
            var notFoundResult = result as NotFoundResult;
            Assert.NotNull(notFoundResult);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [RetryFact(5, 250)]
        public async Task GivenADestinationName_WhenCEchoIsCalledWithAnError_Returns502()
        {
            _repository.Setup(p => p.FindAsync(It.IsAny<string>()))
                .ReturnsAsync(new DestinationApplicationEntity
                {
                    AeTitle = "AET",
                    HostIp = "1.2.3.4",
                    Port = 104,
                    Name = "AET"
                });
            _scuQueue.Setup(p => p.Queue(It.IsAny<ScuWorkRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScuWorkResponse
                {
                    Status = ResponseStatus.Failure,
                    Error = ResponseError.AssociationRejected,
                    Message = "error"
                });
            var result = await _controller.CEcho("AET");
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            var problemDetails = objectResult.Value as ProblemDetails;
            Assert.NotNull(problemDetails);
            Assert.Equal(StatusCodes.Status502BadGateway, problemDetails.Status);
            Assert.Equal("C-ECHO Failure", problemDetails.Title);
            Assert.Equal("error", problemDetails.Detail);
        }

        [RetryFact(5, 250)]
        public async Task GivenADestinationName_WhenCEchoIsCalledWithUnhandledError_Returns500()
        {
            _repository.Setup(p => p.FindAsync(It.IsAny<string>()))
                .ReturnsAsync(new DestinationApplicationEntity
                {
                    AeTitle = "AET",
                    HostIp = "1.2.3.4",
                    Port = 104,
                    Name = "AET"
                });
            _scuQueue.Setup(p => p.Queue(It.IsAny<ScuWorkRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("error"));
            var result = await _controller.CEcho("AET");
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            var problemDetails = objectResult.Value as ProblemDetails;
            Assert.NotNull(problemDetails);
            Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
            Assert.Equal("Error performing C-ECHO", problemDetails.Title);
            Assert.Equal("error", problemDetails.Detail);
        }

        [RetryFact(5, 250)]
        public async Task GivenADestinationName_WhenCEchoIsCalledSuccessfully_Returns200()
        {
            _repository.Setup(p => p.FindAsync(It.IsAny<string>()))
                .ReturnsAsync(new DestinationApplicationEntity
                {
                    AeTitle = "AET",
                    HostIp = "1.2.3.4",
                    Port = 104,
                    Name = "AET"
                });
            _scuQueue.Setup(p => p.Queue(It.IsAny<ScuWorkRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScuWorkResponse
                {
                    Status = ResponseStatus.Success,
                    Error = ResponseError.None,
                    Message = ""
                });
            var result = await _controller.CEcho("AET");
            var okResult = result as OkResult;
            Assert.NotNull(okResult);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        }

        #endregion C-Echo

        #region Create

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return problem on validation failure")]
        public async Task Create_ShallReturnBadRequestWithBadJobProcessType()
        {
            var aeTitle = "TOOOOOOOOOOOOOOOOOOOOOOOLONG";
            var aeTitles = new DestinationApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                HostIp = "host",
                Port = 1
            };

            var result = await _controller.Create(aeTitles);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Validation error", problem.Title);
            Assert.Equal($"'{aeTitle}' is not a valid AE Title (source: DestinationApplicationEntity).", problem.Detail);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        }

        [RetryFact(5, 250, DisplayName = "Create - Shall return Conflict if entity already exists")]
        public async Task Create_ShallReturnConflictIfEntityAlreadyExists()
        {
            var aeTitle = "AET";
            var aeTitles = new DestinationApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                HostIp = "host",
                Port = 1
            };

            _repository.Setup(p => p.Any(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(true);

            var result = await _controller.Create(aeTitles);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("DICOM destination already exists", problem.Title);
            Assert.Equal("A DICOM destination with the same name 'AET' already exists.", problem.Detail);
            Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);

            _repository.Verify(p => p.AddAsync(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [RetryFact(5, 250, DisplayName = "Create - Shall return problem if failed to add")]
        public async Task Create_ShallReturnBadRequestOnAddFailure()
        {
            var aeTitle = "AET";
            var aeTitles = new DestinationApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                HostIp = "host",
                Port = 1
            };

            _repository.Setup(p => p.AddAsync(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.Create(aeTitles);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error adding new DICOM destination", problem.Title);
            Assert.Equal($"error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);

            _repository.Verify(p => p.AddAsync(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Create - Shall return CreatedAtAction")]
        public async Task Create_ShallReturnCreatedAtAction()
        {
            var aeTitle = "AET";
            var aeTitles = new DestinationApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                HostIp = "host",
                Port = 1
            };

            _repository.Setup(p => p.AddAsync(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()));
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()));

            var result = await _controller.Create(aeTitles);

            Assert.IsType<CreatedAtActionResult>(result.Result);

            _repository.Verify(p => p.AddAsync(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion Create

        #region Update

        [RetryFact(5, 250, DisplayName = "Update - Shall return updated")]
        public async Task Update_ReturnsUpdated()
        {
            var entity = new DestinationApplicationEntity
            {
                AeTitle = "AET",
                HostIp = "host",
                Name = "AET",
                Port = 123,
            };

            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.Remove(It.IsAny<DestinationApplicationEntity>()));
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()));
            _repository.Setup(p => p.Update(It.IsAny<DestinationApplicationEntity>()));

            var result = await _controller.Edit(entity);
            var okResult = result.Result as OkObjectResult;
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var updatedEntity = okResult.Value as DestinationApplicationEntity;

            Assert.Equal(entity.AeTitle, updatedEntity.AeTitle);
            Assert.Equal(entity.HostIp, updatedEntity.HostIp);
            Assert.Equal(entity.Name, updatedEntity.Name);
            Assert.Equal(entity.Port, updatedEntity.Port);

            _repository.Verify(p => p.FindAsync(entity.Name), Times.Once());
            _repository.Verify(p => p.Update(entity), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall return 404 if input is null")]
        public async Task Update_Returns404IfInputIsNull()
        {
            var result = await _controller.Edit(null);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall return 404 if not found")]
        public async Task Update_Returns404IfNotFound()
        {
            var entity = new DestinationApplicationEntity
            {
                AeTitle = "AET",
                HostIp = "host",
                Name = "AET",
                Port = 123,
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(default(DestinationApplicationEntity)));

            var result = await _controller.Edit(entity);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindAsync(entity.Name), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall return problem on failure")]
        public async Task Update_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            var entity = new DestinationApplicationEntity
            {
                AeTitle = value,
                HostIp = "host",
                Name = value,
                Port = 123,
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.Update(It.IsAny<DestinationApplicationEntity>())).Throws(new Exception("error"));

            var result = await _controller.Edit(entity);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error updating DICOM destination.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall return problem on validation failure")]
        public async Task Update_ShallReturnBadRequestWithBadAeTitle()
        {
            var aeTitle = "TOOOOOOOOOOOOOOOOOOOOOOOLONG";
            var entity = new DestinationApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                HostIp = "host",
                Port = 1
            };

            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(entity));
            var result = await _controller.Edit(entity);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Validation error.", problem.Title);
            Assert.Equal($"'{aeTitle}' is not a valid AE Title (source: DestinationApplicationEntity).", problem.Detail);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        }

        #endregion Update

        #region Delete

        [RetryFact(5, 250, DisplayName = "Delete - Shall return deleted object")]
        public async Task Delete_ReturnsDeleted()
        {
            var value = "AET";
            var entity = new DestinationApplicationEntity
            {
                AeTitle = value,
                Name = value
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(entity));

            _repository.Setup(p => p.Remove(It.IsAny<DestinationApplicationEntity>()));
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()));

            var result = await _controller.Delete(value);
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as DestinationApplicationEntity;
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
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(default(DestinationApplicationEntity)));

            var result = await _controller.Delete(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Delete - Shall return problem on failure")]
        public async Task Delete_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            var entity = new DestinationApplicationEntity
            {
                AeTitle = value,
                Name = value
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.Remove(It.IsAny<DestinationApplicationEntity>())).Throws(new Exception("error"));

            var result = await _controller.Delete(value);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error deleting DICOM destination.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        #endregion Delete
    }
}
