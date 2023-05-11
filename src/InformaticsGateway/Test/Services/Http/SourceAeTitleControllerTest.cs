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
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class SourceAeTitleControllerTest
    {
        private static readonly string TestUsername = "test-user";
        private readonly SourceAeTitleController _controller;
        private readonly Mock<ProblemDetailsFactory> _problemDetailsFactory;
        private readonly Mock<ILogger<SourceAeTitleController>> _logger;
        private readonly Mock<ISourceApplicationEntityRepository> _repository;

        public SourceAeTitleControllerTest()
        {
            _logger = new Mock<ILogger<SourceAeTitleController>>();

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

            var controllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            _repository = new Mock<ISourceApplicationEntityRepository>();

            _controller = new SourceAeTitleController(
                 _logger.Object,
                 _repository.Object)
            {
                ProblemDetailsFactory = _problemDetailsFactory.Object,
                ControllerContext = controllerContext
            };
        }

        #region Get

        [RetryFact(5, 250, DisplayName = "Get - Shall return available source AETs")]
        public async Task Get_ShallReturnAllSourceAets()
        {
            var data = new List<SourceApplicationEntity>();
            for (var i = 1; i <= 5; i++)
            {
                data.Add(new SourceApplicationEntity()
                {
                    AeTitle = $"AET{i}",
                    HostIp = "host",
                    Name = $"AET{i}",
                });
            }

            _repository.Setup(p => p.ToListAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(data));

            var result = await _controller.Get();
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as IEnumerable<SourceApplicationEntity>;
            Assert.Equal(data.Count, response.Count());
            _repository.Verify(p => p.ToListAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Get - Shall return problem on failure")]
        public async Task Get_ShallReturnProblemOnFailure()
        {
            _repository.Setup(p => p.ToListAsync(It.IsAny<CancellationToken>())).Throws(new Exception("error"));

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
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(
                Task.FromResult(
                new SourceApplicationEntity
                {
                    AeTitle = value,
                    HostIp = "host",
                    Name = value,
                }));

            var result = await _controller.GetAeTitle(value);
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as SourceApplicationEntity;
            Assert.Equal(value, response.AeTitle);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return matching object")]
        public async Task GetAeTitle_ViaAETitleReturnsAMatch()
        {
            var value = "AET";
            _repository.Setup(p => p.FindByAETAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(
                Task.FromResult(
                new SourceApplicationEntity[]{
                new SourceApplicationEntity
                {
                    AeTitle = value,
                    HostIp = "host",
                    Name = $"{value}name",
                }}));

            var result = await _controller.GetAeTitleByAET(value);
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as SourceApplicationEntity[];
            Assert.Equal(value, response.FirstOrDefault().AeTitle);
            _repository.Verify(p => p.FindByAETAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return 404 if not found")]
        public async Task GetAeTitle_Returns404IfNotFound()
        {
            var value = "AET";
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(default(SourceApplicationEntity)));

            var result = await _controller.GetAeTitle(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return problem on failure")]
        public async Task GetAeTitle_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.GetAeTitle(value);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error querying DICOM sources.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "GetAeTitle from AETitle - Shall return problem on failure")]
        public async Task GetAeTitleViaAETitle_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            _repository.Setup(p => p.FindByAETAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.GetAeTitleByAET(value);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error querying DICOM sources.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindByAETAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion GetAeTitle

        #region Create

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return problem on validation failure")]
        public async Task Create_ShallReturnBadRequestWithBadJobProcessType()
        {
            var aeTitle = "TOOOOOOOOOOOOOOOOOOOOOOOLONG";
            var aeTitles = new SourceApplicationEntity
            {
                AeTitle = aeTitle,
                HostIp = "host",
                Name = aeTitle,
            };

            var result = await _controller.Create(aeTitles);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Validation error.", problem.Title);
            Assert.Equal($"'{aeTitle}' is not a valid AE Title (source: SourceApplicationEntity).", problem.Detail);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        }

        [RetryFact(5, 250, DisplayName = "Create - Shall return conflict if entity already exists")]
        public async Task Create_ShallReturnConflictIfEntityAlreadyExists()
        {
            var aeTitle = "AET";
            var aeTitles = new SourceApplicationEntity
            {
                AeTitle = aeTitle,
                HostIp = "host",
                Name = aeTitle,
            };

            _repository.Setup(p => p.ContainsAsync(It.IsAny<Expression<Func<SourceApplicationEntity, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _controller.Create(aeTitles);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("DICOM source already exists", problem.Title);
            Assert.Equal("A DICOM source with the same name 'AET' already exists.", problem.Detail);
            Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);

            _repository.Verify(p => p.AddAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [RetryFact(5, 250, DisplayName = "Create - Shall return problem if failed to add")]
        public async Task Create_ShallReturnBadRequestOnAddFailure()
        {
            var aeTitle = "AET";
            var aeTitles = new SourceApplicationEntity
            {
                AeTitle = aeTitle,
                HostIp = "host",
                Name = aeTitle,
            };

            _repository.Setup(p => p.AddAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.Create(aeTitles);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error adding new DICOM source.", problem.Title);
            Assert.Equal($"error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);

            _repository.Verify(p => p.AddAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Create - Shall return CreatedAtAction")]
        public async Task Create_ShallReturnCreatedAtAction()
        {
            var aeTitle = "AET";
            var aeTitles = new SourceApplicationEntity
            {
                AeTitle = aeTitle,
                HostIp = "host",
                Name = aeTitle,
            };

            _repository.Setup(p => p.AddAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()));

            var result = await _controller.Create(aeTitles);

            Assert.IsType<CreatedAtActionResult>(result.Result);

            _repository.Verify(p => p.AddAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion Create

        #region Update

        [RetryFact(5, 250, DisplayName = "Update - Shall return updated")]
        public async Task Update_ReturnsUpdated()
        {
            var entity = new SourceApplicationEntity
            {
                AeTitle = "AET",
                HostIp = "host",
                Name = "AET",
            };

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.UpdateAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()));

            var result = await _controller.Edit(entity);
            var okResult = result.Result as OkObjectResult;
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var updatedEntity = okResult.Value as SourceApplicationEntity;

            Assert.Equal(entity.AeTitle, updatedEntity.AeTitle);
            Assert.Equal(entity.HostIp, updatedEntity.HostIp);
            Assert.Equal(entity.Name, updatedEntity.Name);
            Assert.NotNull(updatedEntity.DateTimeUpdated);
            Assert.Null(updatedEntity.UpdatedBy);

            _repository.Verify(p => p.FindByNameAsync(entity.Name, It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall return updated when user is authenticated")]
        public async Task Update_ReturnsUpdatedWhenUserIsAuthenticated()
        {
            var controllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal(new GenericIdentity(TestUsername)) } };
            var controller = new SourceAeTitleController(
                 _logger.Object,
                 _repository.Object)
            {
                ProblemDetailsFactory = _problemDetailsFactory.Object,
                ControllerContext = controllerContext
            };

            var entity = new SourceApplicationEntity
            {
                AeTitle = "AET",
                HostIp = "host",
                Name = "AET",
            };

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.UpdateAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()));

            var result = await controller.Edit(entity);
            var okResult = result.Result as OkObjectResult;
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var updatedEntity = okResult.Value as SourceApplicationEntity;

            Assert.Equal(entity.AeTitle, updatedEntity.AeTitle);
            Assert.Equal(entity.HostIp, updatedEntity.HostIp);
            Assert.Equal(entity.Name, updatedEntity.Name);
            Assert.NotNull(updatedEntity.DateTimeUpdated);
            Assert.Equal(TestUsername, updatedEntity.UpdatedBy);

            _repository.Verify(p => p.FindByNameAsync(entity.Name, It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once());
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
            var entity = new SourceApplicationEntity
            {
                AeTitle = "AET",
                HostIp = "host",
                Name = "AET",
            };
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(default(SourceApplicationEntity)));

            var result = await _controller.Edit(entity);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindByNameAsync(entity.Name, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall return problem on failure")]
        public async Task Update_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            var entity = new SourceApplicationEntity
            {
                AeTitle = value,
                HostIp = "host",
                Name = value,
            };
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.UpdateAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.Edit(entity);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error updating DICOM source.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall return problem on validation failure")]
        public async Task Update_ShallReturnBadRequestWithBadAeTitle()
        {
            var aeTitle = "TOOOOOOOOOOOOOOOOOOOOOOOLONG";
            var entity = new SourceApplicationEntity
            {
                Name = aeTitle,
                AeTitle = aeTitle,
                HostIp = "host",
            };

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));
            var result = await _controller.Edit(entity);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Validation error.", problem.Title);
            Assert.Equal($"'{aeTitle}' is not a valid AE Title (source: SourceApplicationEntity).", problem.Detail);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        }

        #endregion Update

        #region Delete

        [RetryFact(5, 250, DisplayName = "Delete - Shall return deleted object")]
        public async Task Delete_ReturnsDeleted()
        {
            var value = "AET";
            var entity = new SourceApplicationEntity
            {
                AeTitle = value,
                HostIp = "host",
                Name = value,
            };
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));

            _repository.Setup(p => p.RemoveAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()));

            var result = await _controller.Delete(value);
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as SourceApplicationEntity;
            Assert.Equal(value, response.AeTitle);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.RemoveAsync(entity, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Delete - Shall return 404 if not found")]
        public async Task Delete_Returns404IfNotFound()
        {
            var value = "AET";
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(default(SourceApplicationEntity)));

            var result = await _controller.Delete(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Delete - Shall return problem on failure")]
        public async Task Delete_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            var entity = new SourceApplicationEntity
            {
                AeTitle = value,
                HostIp = "host",
                Name = value,
            };
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.RemoveAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.Delete(value);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error deleting DICOM source.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion Delete
    }
}
