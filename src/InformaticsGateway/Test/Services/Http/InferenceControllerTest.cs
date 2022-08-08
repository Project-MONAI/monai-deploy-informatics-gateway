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
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class InferenceControllerTest
    {
        private readonly Mock<IInferenceRequestRepository> _inferenceRequestRepository;
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<ILogger<InferenceController>> _logger;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly InferenceController _controller;
        private readonly Mock<ProblemDetailsFactory> _problemDetailsFactory;

        public InferenceControllerTest()
        {
            _inferenceRequestRepository = new Mock<IInferenceRequestRepository>();
            _informaticsGatewayConfiguration = new InformaticsGatewayConfiguration();
            _configuration = Options.Create(_informaticsGatewayConfiguration);
            _logger = new Mock<ILogger<InferenceController>>();
            _fileSystem = new Mock<IFileSystem>();
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
            _controller = new InferenceController(_inferenceRequestRepository.Object, _configuration, _logger.Object)
            {
                ProblemDetailsFactory = _problemDetailsFactory.Object
            };
        }

        [RetryFact(5, 250, DisplayName = "NewInferenceRequest - shall return problem if input is invalid")]
        public void NewInferenceRequest_ShallReturnProblemIfInputIsInvalid()
        {
            var input = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString()
            };

            var result = _controller.NewInferenceRequest(input);

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Invalid request", problem.Title);
            Assert.Equal(422, problem.Status);
        }

        [RetryFact(5, 250, DisplayName = "NewInferenceRequest - shall return problem if output is invalid")]
        public void NewInferenceRequest_ShallReturnProblemIfOutputIsInvalid()
        {
            var input = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
                InputResources = new List<RequestInputDataResource>()
                {
                    new RequestInputDataResource
                    {
                        Interface = InputInterfaceType.Algorithm,
                        ConnectionDetails = new InputConnectionDetails()
                    },
                    new RequestInputDataResource
                    {
                        Interface = InputInterfaceType.DicomWeb,
                        ConnectionDetails = new InputConnectionDetails()
                    }
                },
                OutputResources = new List<RequestOutputDataResource>()
                {
                    new RequestOutputDataResource
                    {
                        Interface = InputInterfaceType.DicomWeb,
                        ConnectionDetails = new InputConnectionDetails
                        {
                             AuthType = ConnectionAuthType.Bearer
                        }
                    }
                }
            };

            var result = _controller.NewInferenceRequest(input);

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Invalid request", problem.Title);
            Assert.Equal(422, problem.Status);
        }

        [RetryFact(5, 250, DisplayName = "NewInferenceRequest - shall return problem if same transactionId exits")]
        public void NewInferenceRequest_ShallReturnProblemIfSameTransactionIdExists()
        {
            _inferenceRequestRepository.Setup(p => p.Exists(It.IsAny<string>())).Returns(true);

            var input = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
                InputResources = new List<RequestInputDataResource>()
                {
                    new RequestInputDataResource
                    {
                        Interface = InputInterfaceType.Algorithm,
                        ConnectionDetails = new InputConnectionDetails()
                    },
                    new RequestInputDataResource
                    {
                        Interface = InputInterfaceType.DicomWeb,
                        ConnectionDetails = new InputConnectionDetails
                        {
                            Uri = "http://my.svc/api"
                        }
                    }
                },
                InputMetadata = new InferenceRequestMetadata
                {
                    Details = new InferenceRequestDetails
                    {
                        Type = InferenceRequestType.DicomUid,
                        Studies = new List<RequestedStudy>
                    {
                        new RequestedStudy
                        {
                            StudyInstanceUid = "1"
                        }
                    }
                    }
                }
            };

            var result = _controller.NewInferenceRequest(input);

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Conflict", problem.Title);
            Assert.Equal(409, problem.Status);
        }

        //[RetryFact(5, 250, DisplayName = "NewInferenceRequest - shall return problem if failed to creaet working dir")]
        //public void NewInferenceRequest_ShallReturnProblemIfFailedToCreateWorkingDir()
        //{
        //    _fileSystem.Setup(p => p.Directory.CreateDirectory(It.IsAny<string>()))
        //        .Throws(new IOException());
        //    _fileSystem.Setup(p => p.Path.Combine(It.IsAny<string>(), It.IsAny<string>())).Returns((string path1, string path2) => System.IO.Path.Combine(path1, path2));

        //    var input = new InferenceRequest
        //    {
        //        TransactionId = Guid.NewGuid().ToString(),
        //        InputResources = new List<RequestInputDataResource>()
        //        {
        //            new RequestInputDataResource
        //            {
        //                Interface = InputInterfaceType.Algorithm,
        //                ConnectionDetails = new InputConnectionDetails()
        //            },
        //            new RequestInputDataResource
        //            {
        //                Interface = InputInterfaceType.DicomWeb,
        //                ConnectionDetails = new InputConnectionDetails
        //                {
        //                    Uri = "http://my.svc/api"
        //                }
        //            }
        //        },
        //        InputMetadata = new InferenceRequestMetadata
        //        {
        //            Details = new InferenceRequestDetails
        //            {
        //                Type = InferenceRequestType.DicomUid,
        //                Studies = new List<RequestedStudy>
        //            {
        //                new RequestedStudy
        //                {
        //                    StudyInstanceUid = "1"
        //                }
        //            }
        //            }
        //        }
        //    };

        //    var result = _controller.NewInferenceRequest(input);

        //    Assert.NotNull(result);
        //    var objectResult = result.Result as ObjectResult;
        //    Assert.NotNull(objectResult);
        //    var problem = objectResult.Value as ProblemDetails;
        //    Assert.NotNull(problem);
        //    Assert.Equal("Failed to generate a temporary storage location for request.", problem.Title);
        //    Assert.Equal(500, problem.Status);
        //}

        [RetryFact(5, 250, DisplayName = "NewInferenceRequest - shall return problem if failed to add job")]
        public void NewInferenceRequest_ShallReturnProblemIfFailedToAddJob()
        {
            _fileSystem.Setup(p => p.Directory.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(p => p.Path.Combine(It.IsAny<string>(), It.IsAny<string>())).Returns((string path1, string path2) => System.IO.Path.Combine(path1, path2));
            _inferenceRequestRepository.Setup(p => p.Add(It.IsAny<InferenceRequest>()))
                .Throws(new Exception("error"));

            var input = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
                InputResources = new List<RequestInputDataResource>()
                {
                    new RequestInputDataResource
                    {
                        Interface = InputInterfaceType.Algorithm,
                        ConnectionDetails = new InputConnectionDetails()
                    },
                    new RequestInputDataResource
                    {
                        Interface = InputInterfaceType.DicomWeb,
                        ConnectionDetails = new InputConnectionDetails
                        {
                            Uri = "http://my.svc/api"
                        }
                    }
                },
                InputMetadata = new InferenceRequestMetadata
                {
                    Details = new InferenceRequestDetails
                    {
                        Type = InferenceRequestType.DicomUid,
                        Studies = new List<RequestedStudy>
                    {
                        new RequestedStudy
                        {
                            StudyInstanceUid = "1"
                        }
                    }
                    }
                }
            };

            var result = _controller.NewInferenceRequest(input);

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Failed to save request", problem.Title);
            Assert.Equal(500, problem.Status);
        }

        [RetryFact(5, 250, DisplayName = "NewInferenceRequest - shall accept inference request")]
        public void NewInferenceRequest_ShallAcceptInferenceRequest()
        {
            _fileSystem.Setup(p => p.Directory.CreateDirectory(It.IsAny<string>()));
            _fileSystem.Setup(p => p.Path.Combine(It.IsAny<string>(), It.IsAny<string>())).Returns((string path1, string path2) => System.IO.Path.Combine(path1, path2));
            _inferenceRequestRepository.Setup(p => p.Add(It.IsAny<InferenceRequest>()));

            var input = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
                InputResources = new List<RequestInputDataResource>()
                {
                    new RequestInputDataResource
                    {
                        Interface = InputInterfaceType.Algorithm,
                        ConnectionDetails = new InputConnectionDetails()
                    },
                    new RequestInputDataResource
                    {
                        Interface = InputInterfaceType.DicomWeb,
                        ConnectionDetails = new InputConnectionDetails
                        {
                            Uri = "http://my.svc/api"
                        }
                    }
                },
                InputMetadata = new InferenceRequestMetadata
                {
                    Details = new InferenceRequestDetails
                    {
                        Type = InferenceRequestType.DicomUid,
                        Studies = new List<RequestedStudy>
                    {
                        new RequestedStudy
                        {
                            StudyInstanceUid = "1"
                        }
                    }
                    }
                }
            };

            var result = _controller.NewInferenceRequest(input);

            _inferenceRequestRepository.Verify(p => p.Add(input), Times.Once());

            Assert.NotNull(result);
            var objectResult = result.Result as OkObjectResult;
            Assert.NotNull(objectResult);
            var response = objectResult.Value as InferenceRequestResponse;
            Assert.NotNull(response);
            Assert.Equal(input.TransactionId, response.TransactionId);
        }

        [RetryFact(5, 250, DisplayName = "Status - return 404 if not found")]
        public void Status_NotFound()
        {
            _inferenceRequestRepository.Setup(p => p.GetStatus(It.IsAny<string>()))
                .Returns(Task.FromResult((InferenceStatusResponse)null));

            var jobId = Guid.NewGuid().ToString();
            var result = _controller.JobStatus(jobId);

            _inferenceRequestRepository.Verify(p => p.GetStatus(jobId), Times.Once());

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Inference request not found.", problem.Title);
            Assert.Equal(404, problem.Status);
        }

        [RetryFact(5, 250, DisplayName = "Status - return 500 on error")]
        public void Status_ShallReturnProblemException()
        {
            _inferenceRequestRepository.Setup(p => p.GetStatus(It.IsAny<string>()))
                .Throws(new Exception("error"));

            var jobId = Guid.NewGuid().ToString();
            var result = _controller.JobStatus(jobId);

            _inferenceRequestRepository.Verify(p => p.GetStatus(jobId), Times.Once());

            Assert.NotNull(result);
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Failed to retrieve inference request status.", problem.Title);
            Assert.Equal(500, problem.Status);
        }

        [RetryFact(5, 250, DisplayName = "Status - returns 200")]
        public void Status_ReturnsStatus()
        {
            _inferenceRequestRepository.Setup(p => p.GetStatus(It.IsAny<string>()))
                .Returns(Task.FromResult(
                    new InferenceStatusResponse
                    {
                        TransactionId = "TRANSACTIONID"
                    }));

            var jobId = Guid.NewGuid().ToString();
            var result = _controller.JobStatus(jobId);

            _inferenceRequestRepository.Verify(p => p.GetStatus(jobId), Times.Once());

            Assert.NotNull(result);
            var objectResult = result.Result as OkObjectResult;
            Assert.NotNull(objectResult);
            var response = objectResult.Value as InferenceStatusResponse;
            Assert.NotNull(response);
            Assert.Equal("TRANSACTIONID", response.TransactionId);
        }
    }
}
