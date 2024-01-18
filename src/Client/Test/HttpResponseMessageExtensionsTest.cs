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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Client.Services;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Client.Test
{
    public class HttpResponseMessageExtensionsTest
    {
        [Fact(DisplayName = "Success status code")]
        public async Task SuccessStatusCode()
        {
            var message = new HttpResponseMessage(HttpStatusCode.OK);
            var exception = await Record.ExceptionAsync(async () => await message.EnsureSuccessStatusCodeWithProblemDetails()).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.Null(exception);
        }

        [Fact(DisplayName = "Returns problem details")]
        public async Task ReturnProblemDetails()
        {
            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var message = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var exception = await Assert.ThrowsAsync<ProblemException>(async () => await message.EnsureSuccessStatusCodeWithProblemDetails());

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", exception.Message);
        }

        [Fact(DisplayName = "Returns other errors")]
        public async Task ReturnOtherErrors()
        {
            var logger = new Mock<ILogger>();
            var message = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("error message", Encoding.UTF8, "application/json")
            };

            var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await message.EnsureSuccessStatusCodeWithProblemDetails(logger.Object));

            Assert.Equal("error message", exception.Message);
            logger.VerifyLogging("Error reading server side problem.", LogLevel.Trace, Times.Once());
        }

        [Fact(DisplayName = "Returns other errors (JSON)")]
        public async Task ReturnOtherJsonErrors()
        {
            var unhandledException = new Exception("error message");
            var json = JsonSerializer.Serialize(unhandledException, Configuration.JsonSerializationOptions);

            var logger = new Mock<ILogger>();
            var message = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await message.EnsureSuccessStatusCodeWithProblemDetails(logger.Object));

            Assert.Equal(json, exception.Message);
        }
    }
}
