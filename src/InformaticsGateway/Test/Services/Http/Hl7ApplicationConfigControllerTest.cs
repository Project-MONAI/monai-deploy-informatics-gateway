/*
 * Copyright 2021-2023 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class Hl7ApplicationConfigControllerTest
    {
        private readonly Mock<ILogger<Hl7ApplicationConfigController>> _logger;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Hl7ApplicationConfigController _controller;
        private readonly Mock<IHl7ApplicationConfigRepository> _repo;

        public Hl7ApplicationConfigControllerTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger<Hl7ApplicationConfigController>>();
            _repo = new Mock<IHl7ApplicationConfigRepository>();
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);

            _controller = new Hl7ApplicationConfigController(
                _logger.Object, _repo.Object);
        }

        private static Hl7ApplicationConfigEntity ValidApplicationEntity(string dicomStr)
        {
            var validApplicationEntity = new Hl7ApplicationConfigEntity()
            {
                Id = Guid.Empty,
                DataLink = KeyValuePair.Create("testKey", DataLinkType.PatientId),
                DataMapping = new() { KeyValuePair.Create("datamapkey", dicomStr) },
                SendingId = KeyValuePair.Create("sendingidkey", "sendingidvalue"),
                DateTimeCreated = DateTime.UtcNow
            };
            return validApplicationEntity;
        }

        #region GET Tests

        [Fact]
        public async Task Get_GiveExpectedInput_ReturnsOK()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Hl7ApplicationConfigEntity>());
            var result = await _controller.Get();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<List<Hl7ApplicationConfigEntity>>(okResult.Value);
            Assert.Empty(response);
        }

        [Fact]
        public async Task Get_GiveExpectedInput_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Hl7ApplicationConfigEntity)null);
            var result = await _controller.Get("test");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Get_GiveExpectedInput_ReturnsOK2()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Hl7ApplicationConfigEntity());
            var result = await _controller.Get("test");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Hl7ApplicationConfigEntity>(okResult.Value);
            Assert.NotNull(response);
        }
        #endregion

        #region DELETE Tests

        [Fact]
        public async Task Delete_GiveExpectedInput_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Hl7ApplicationConfigEntity)null);
            var result = await _controller.Delete("test");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_GiveExpectedInput_ReturnsOK()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Hl7ApplicationConfigEntity());
            _repo.Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Hl7ApplicationConfigEntity());
            var result = await _controller.Delete("test");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Hl7ApplicationConfigEntity>(okResult.Value);
            Assert.NotNull(response);
        }

        [Fact]
        public async Task Delete_GiveExpectedInput_ReturnsInternalServerError()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Hl7ApplicationConfigEntity());
            _repo.Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new DatabaseException());
            var result = await _controller.Delete("test");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task Delete_GiveExpectedInput_ReturnsInternalServerError2()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Hl7ApplicationConfigEntity());
            _repo.Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception());
            var result = await _controller.Delete("test");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        #endregion

        #region PUT Tests

        [Fact]
        public async Task Put_GiveExpectedInput_ReturnsOK()
        {
            var validApplicationEntity = ValidApplicationEntity("0001,0001");
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(validApplicationEntity);
            _repo.Setup(r => r.CreateAsync(It.IsAny<Hl7ApplicationConfigEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validApplicationEntity);
            var result = await _controller.Put(validApplicationEntity);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Guid>(okResult.Value);
            Assert.NotNull(response);
        }

        [Fact]
        public async Task Put_GiveExpectedInput_ReturnsBadRequest()
        {
            var result = await _controller.Put(null!);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task Put_GiveExpectedInput_ReturnsBadRequest2()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Hl7ApplicationConfigEntity());
            var result = await _controller.Put(new Hl7ApplicationConfigEntity());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task Put_GiveExpectedInput_ReturnsInternalServerError()
        {
            var validApplicationEntity = ValidApplicationEntity("0001,0001");
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(validApplicationEntity);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<Hl7ApplicationConfigEntity>(), It.IsAny<CancellationToken>()))
                .Throws(new DatabaseException());
            var result = await _controller.Put(validApplicationEntity);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        #endregion

        #region POST Tests

        [Theory]
        [InlineData("(0001,0001)")]
        [InlineData("0001,0001")]
        [InlineData("(FFFE,E0DD)")]
        [InlineData("FFFE,E0DD")]
        public async Task Post_GiveExpectedInput_ReturnsOK(string dicomStr)
        {
            var validApplicationEntity = ValidApplicationEntity(dicomStr);

            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(validApplicationEntity);
            _repo.Setup(r => r.CreateAsync(It.IsAny<Hl7ApplicationConfigEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validApplicationEntity);
            var result = await _controller.Post(validApplicationEntity);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Hl7ApplicationConfigEntity>(okResult.Value);
            Assert.NotNull(response);
        }

        [Theory]
        [InlineData("(001,0001)")]
        [InlineData("(0001,00x1")]
        [InlineData("x001,0001)")]
        [InlineData("00001,00001)")]
        public async Task Post_GivenInvalidDicomValueInput_ReturnsBadRequest(string dicomStr)
        {
            //valid Hl7ApplicationEntity
            var validApplicationEntity = new Hl7ApplicationConfigEntity()
            {
                Id = Guid.Empty,
                DataLink = KeyValuePair.Create("testKey", DataLinkType.PatientId),
                DataMapping = new() { KeyValuePair.Create("datamapkey", dicomStr) },
                SendingId = KeyValuePair.Create("sendingidkey", "sendingidvalue"),
                DateTimeCreated = DateTime.UtcNow
            };

            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(validApplicationEntity);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<Hl7ApplicationConfigEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validApplicationEntity);
            var result = await _controller.Post(validApplicationEntity);

            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objResult.StatusCode);
        }

        [Fact]
        public async Task Post_GiveExpectedInput_ReturnsBadRequest()
        {
            var result = await _controller.Post(null!);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task Post_GiveExpectedInput_ReturnsBadRequest2()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Hl7ApplicationConfigEntity)null);
            var result = await _controller.Post(new Hl7ApplicationConfigEntity());

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);

        }

        [Fact]
        public async Task Post_GiveExpectedInput_ReturnsInternalServerError()
        {
            var validApplicationEntity = ValidApplicationEntity("0001,0001");
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(validApplicationEntity);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<Hl7ApplicationConfigEntity>(), It.IsAny<CancellationToken>()))
                .Throws(new DatabaseException());
            var result = await _controller.Post(validApplicationEntity);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }
        [Fact]
        public async Task Post_GiveExpectedInput_ReturnsInternalServerError3()
        {
            var validApplicationEntity = ValidApplicationEntity("0001,0001");
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(validApplicationEntity);

            var result = await _controller.Post(validApplicationEntity);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task Post_GiveExpectedInput_ReturnsInternalServerError2()
        {
            var validApplicationEntity = ValidApplicationEntity("0001,0001");
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(validApplicationEntity);
            _repo.Setup(r => r.CreateAsync(It.IsAny<Hl7ApplicationConfigEntity>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception());
            var result = await _controller.Post(validApplicationEntity);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        #endregion
    }
}
