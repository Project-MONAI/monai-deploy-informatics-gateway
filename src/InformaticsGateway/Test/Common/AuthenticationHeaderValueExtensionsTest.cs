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

using Monai.Deploy.InformaticsGateway.Services.Common;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Common
{
    public class AuthenticationHeaderValueExtensionsTest
    {
        [RetryFact(5, 250, DisplayName = "ConvertFrom - Basic")]
        public void ConvertFromBasic()
        {
            var result = AuthenticationHeaderValueExtensions.ConvertFrom(Api.Rest.ConnectionAuthType.Basic, "test");
            Assert.Equal("Basic", result.Scheme);
            Assert.Equal("test", result.Parameter);
        }

        [RetryFact(5, 250, DisplayName = "ConvertFrom - Bearer")]
        public void ConvertFromBearer()
        {
            var result = AuthenticationHeaderValueExtensions.ConvertFrom(Api.Rest.ConnectionAuthType.Bearer, "test");
            Assert.Equal("Bearer", result.Scheme);
            Assert.Equal("test", result.Parameter);
        }

        [RetryFact(5, 250, DisplayName = "ConvertFrom - None")]
        public void ConvertFromNone()
        {
            var result = AuthenticationHeaderValueExtensions.ConvertFrom(Api.Rest.ConnectionAuthType.None, "test");
            Assert.Null(result);
        }
    }
}
