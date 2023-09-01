/*
 * Copyright 2023 MONAI Consortium
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
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class VirtualAeTitleControllerTest
    {
        private static readonly string TestUsername = "test-user";
        private readonly VirtualAeTitleController _controller;
        private readonly Mock<ProblemDetailsFactory> _problemDetailsFactory;
        private readonly Mock<ILogger<VirtualAeTitleController>> _logger;
        private readonly Mock<IVirtualApplicationEntityRepository> _repository;
        private readonly Mock<IDataPlugInEngineFactory<IInputDataPlugIn>> _pluginFactory;

        public VirtualAeTitleControllerTest()
        {
            _logger = new Mock<ILogger<VirtualAeTitleController>>();

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

            _repository = new Mock<IVirtualApplicationEntityRepository>();
            _pluginFactory = new Mock<IDataPlugInEngineFactory<IInputDataPlugIn>>();

            var controllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal(new GenericIdentity(TestUsername)) } };
            _controller = new VirtualAeTitleController(
                 _logger.Object,
                 _repository.Object,
                 _pluginFactory.Object)
            {
                ControllerContext = controllerContext,
                ProblemDetailsFactory = _problemDetailsFactory.Object
            };
        }

        #region Get

        [RetryFact(5, 250, DisplayName = "Get - Shall return available MONAI AETs")]
        public async Task Get_ShallReturnAllMonaiAets()
        {
            var data = new List<VirtualApplicationEntity>();
            for (var i = 1; i <= 5; i++)
            {
                data.Add(new VirtualApplicationEntity()
                {
                    VirtualAeTitle = $"AET{i}",
                    Name = $"AET{i}",
                    Workflows = new List<string> { "A", "B" }
                });
            }

            _repository.Setup(p => p.ToListAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(data));

            var result = await _controller.Get();
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as IEnumerable<VirtualApplicationEntity>;
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
                new VirtualApplicationEntity
                {
                    VirtualAeTitle = value,
                    Name = value,
                    Workflows = new List<string> { "A", "B" }
                }));

            var result = await _controller.GetAeTitle(value);
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as VirtualApplicationEntity;
            Assert.NotNull(response);
            Assert.Equal(value, response.VirtualAeTitle);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "GetAeTitle - Shall return 404 if not found")]
        public async Task GetAeTitle_Returns404IfNotFound()
        {
            var value = "AET";
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(default(VirtualApplicationEntity)));

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
            Assert.Equal("Error querying Virtual Application Entity.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion GetAeTitle

        #region Create

        [RetryFact(DisplayName = "Create - Shall return Conflict if entity already exists")]
        public async Task Create_ShallReturnConflictIfIEntityAlreadyExists()
        {
            _repository.Setup(p => p.ContainsAsync(It.IsAny<Expression<Func<VirtualApplicationEntity, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var virtualAeTitle = new VirtualApplicationEntity
            {
                Name = "AET1",
                VirtualAeTitle = "AET1",
                Workflows = new List<string> { "A", "B" }
            };

            var result = await _controller.Create(virtualAeTitle);

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("AE Title already exists", problem.Title);
            Assert.Equal("A Virtual Application Entity with the same name 'AET1' already exists.", problem.Detail);
            Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        }

        [Fact(DisplayName = "Create - Shall return BadRequest when validation fails")]
        public async Task Create_ShallReturnBadRequestOnValidationFailure()
        {
            var virtualAeTitle = new VirtualApplicationEntity
            {
                Name = "AeTitleIsTooooooLooooong",
                VirtualAeTitle = "AeTitleIsTooooooLooooong",
                Workflows = new List<string> { "A", "B" }
            };

            var result = await _controller.Create(virtualAeTitle);

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Validation error.", problem.Title);
            Assert.Equal("'AeTitleIsTooooooLooooong' is not a valid AE Title (source: virtualAeTitle).", problem.Detail);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        }

        [RetryFact(5, 250, DisplayName = "Create - Shall return problem if failed to add")]
        public async Task Create_ShallReturnBadRequestOnAddFailure()
        {
            var aeTitle = "AET";
            var virtualAeTitle = new VirtualApplicationEntity
            {
                Name = aeTitle,
                VirtualAeTitle = aeTitle,
                Workflows = new List<string> { "A", "B" }
            };

            _repository.Setup(p => p.AddAsync(It.IsAny<VirtualApplicationEntity>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.Create(virtualAeTitle);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error adding new Virtual Application Entity.", problem.Title);
            Assert.Equal($"error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);

            _repository.Verify(p => p.AddAsync(It.IsAny<VirtualApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Create - Shall return CreatedAtAction")]
        public async Task Create_ShallReturnCreatedAtAction()
        {
            var aeTitle = "AET";
            var virtualAeTitle = new VirtualApplicationEntity
            {
                Name = aeTitle,
                VirtualAeTitle = aeTitle,
                Workflows = new List<string> { "A", "B" }
            };

            _repository.Setup(p => p.AddAsync(It.IsAny<VirtualApplicationEntity>(), It.IsAny<CancellationToken>()));

            var result = await _controller.Create(virtualAeTitle);

            Assert.IsType<CreatedAtActionResult>(result.Result);

            _repository.Verify(p => p.AddAsync(It.Is<VirtualApplicationEntity>(p => p.CreatedBy == TestUsername), It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion Create

        #region Update

        [RetryFact(DisplayName = "Update - Shall return updated")]
        public async Task Update_ReturnsUpdated()
        {
            var entity = new VirtualApplicationEntity
            {
                VirtualAeTitle = "AET",
                Name = "AET",
                Workflows = new List<string> { "1", "2", "3" },
                PlugInAssemblies = new List<string> { "A", "B", "C" },
            };

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.UpdateAsync(It.IsAny<VirtualApplicationEntity>(), It.IsAny<CancellationToken>()));

            var result = await _controller.Edit(entity);
            var okResult = result.Result as OkObjectResult;
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var updatedEntity = okResult.Value as VirtualApplicationEntity;

            Assert.Equal(entity.VirtualAeTitle, updatedEntity.VirtualAeTitle);
            Assert.Equal(entity.Name, updatedEntity.Name);
            Assert.Equal(entity.Workflows, updatedEntity.Workflows);
            Assert.Equal(entity.PlugInAssemblies, updatedEntity.PlugInAssemblies);
            Assert.NotNull(updatedEntity.DateTimeUpdated);
            Assert.Equal(TestUsername, updatedEntity.UpdatedBy);

            _repository.Verify(p => p.FindByNameAsync(entity.Name, It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(DisplayName = "Update - Shall return updated without updating AE Title")]
        public async Task Update_ReturnsUpdated_WithoutUpdatingAET()
        {
            var originalEntity = new VirtualApplicationEntity
            {
                VirtualAeTitle = "AET",
                Name = "AET",
                Workflows = new List<string> { "1", "2", "3" },
            };

            var entity = new VirtualApplicationEntity
            {
                VirtualAeTitle = "SHOUD-NOT-CHANGE",
                Name = "AET",
                Workflows = new List<string> { "1", "2" },
                PlugInAssemblies = new List<string> { "A", "B" },
            };

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(originalEntity));
            _repository.Setup(p => p.UpdateAsync(It.IsAny<VirtualApplicationEntity>(), It.IsAny<CancellationToken>()));

            var result = await _controller.Edit(entity);
            var okResult = result.Result as OkObjectResult;
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var updatedEntity = okResult.Value as VirtualApplicationEntity;

            Assert.NotEqual(entity.VirtualAeTitle, updatedEntity.VirtualAeTitle);
            Assert.Equal(originalEntity.VirtualAeTitle, updatedEntity.VirtualAeTitle);
            Assert.Equal(entity.Name, updatedEntity.Name);
            Assert.Equal(entity.Workflows, updatedEntity.Workflows);
            Assert.Equal(entity.PlugInAssemblies, updatedEntity.PlugInAssemblies);
            Assert.Collection(entity.PlugInAssemblies, p => p.Equals("A", StringComparison.CurrentCultureIgnoreCase), p => p.Equals("B", StringComparison.CurrentCultureIgnoreCase));
            Assert.NotNull(updatedEntity.DateTimeUpdated);
            Assert.Equal(TestUsername, updatedEntity.UpdatedBy);

            _repository.Verify(p => p.FindByNameAsync(entity.Name, It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.UpdateAsync(updatedEntity, It.IsAny<CancellationToken>()), Times.Once());
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
            var entity = new VirtualApplicationEntity
            {
                VirtualAeTitle = "AET",
                Name = "AET",
                Workflows = new List<string> { "1", "2", "3" },
            };
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(default(VirtualApplicationEntity)));

            var result = await _controller.Edit(entity);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindByNameAsync(entity.Name, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall return problem on failure")]
        public async Task Update_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            var entity = new VirtualApplicationEntity
            {
                VirtualAeTitle = value,
                Name = "AET",
                Workflows = new List<string> { "1", "2", "3" },
            };
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.UpdateAsync(It.IsAny<VirtualApplicationEntity>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.Edit(entity);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error updating Virtual Application Entity.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall return problem on validation failure")]
        public async Task Update_ShallReturnBadRequestWithBadAeTitle()
        {
            var aeTitle = "TOOOOOOOOOOOOOOOOOOOOOOOLONG";
            var entity = new VirtualApplicationEntity
            {
                VirtualAeTitle = aeTitle,
                Name = "AET",
                Workflows = new List<string> { "1", "2", "3" },
            };

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));
            var result = await _controller.Edit(entity);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Validation error.", problem.Title);
            Assert.Equal($"'{aeTitle}' is not a valid AE Title (source: virtualAeTitle).", problem.Detail);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        }

        #endregion Update

        #region Delete

        [RetryFact(5, 250, DisplayName = "Delete - Shall return deleted object")]
        public async Task Delete_ReturnsDeleted()
        {
            var value = "AET";
            var entity = new VirtualApplicationEntity
            {
                VirtualAeTitle = value,
                Name = value
            };
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));

            _repository.Setup(p => p.RemoveAsync(It.IsAny<VirtualApplicationEntity>(), It.IsAny<CancellationToken>()));

            var result = await _controller.Delete(value);
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as VirtualApplicationEntity;
            Assert.NotNull(response);
            Assert.Equal(value, response.VirtualAeTitle);

            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.RemoveAsync(entity, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Delete - Shall return 404 if not found")]
        public async Task Delete_Returns404IfNotFound()
        {
            var value = "AET";
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(default(VirtualApplicationEntity)));

            var result = await _controller.Delete(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Delete - Shall return problem on failure")]
        public async Task Delete_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            var entity = new VirtualApplicationEntity
            {
                VirtualAeTitle = value,
                Name = value
            };
            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.RemoveAsync(It.IsAny<VirtualApplicationEntity>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.Delete(value);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error deleting Virtual Application Entity.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindByNameAsync(value, It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion Delete

        #region GetPlugIns

        [RetryFact(5, 250, DisplayName = "GetPlugIns - Shall return registered plug-ins")]
        public void GetPlugIns_ReturnsRegisteredPlugIns()
        {
            var input = new Dictionary<string, string>() { { "A", "1" }, { "B", "3" }, { "C", "3" } };

            _pluginFactory.Setup(p => p.RegisteredPlugIns()).Returns(input);

            var result = _controller.GetPlugIns();
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as IDictionary<string, string>;
            Assert.NotNull(response);
            Assert.Equal(input, response);

            _pluginFactory.Verify(p => p.RegisteredPlugIns(), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "GetPlugIns - Shall return problem on failure")]
        public void GetPlugIns_ShallReturnProblemOnFailure()
        {
            _pluginFactory.Setup(p => p.RegisteredPlugIns()).Throws(new Exception("error"));

            var result = _controller.GetPlugIns();
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error reading data input plug-ins.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _pluginFactory.Verify(p => p.RegisteredPlugIns(), Times.Once());
        }

        #endregion GetPlugIns
    }
}
