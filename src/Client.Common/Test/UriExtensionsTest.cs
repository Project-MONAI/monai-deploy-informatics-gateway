/*
 * Copyright 2022 MONAI Consortium
 * Copyright 2019-2020 NVIDIA Corporation
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
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Client.Common.Test
{
    public class UriExtensionsTest
    {
        [Theory(DisplayName = "Ensure Uri ends with slash")]
        [InlineData("http://abc.com", "http://abc.com/")]
        [InlineData("http://abc.com/api", "http://abc.com/api/")]
        public void EnsureUriEndsWithSlash(string input, object expected)
        {
            var uri = new Uri(input);

            Assert.Equal(expected, uri.EnsureUriEndsWithSlash().ToString());
        }

        [Fact(DisplayName = "EnsureUriEndsWithSlash shall throw when input is null")]
        public void EnsureUriEndsWithSlash_Null()
        {
            Uri input = null;
            Assert.Throws<ArgumentNullException>(() => input.EnsureUriEndsWithSlash());
        }

        [Fact(DisplayName = "EnsureUriEndsWithSlash shall append slash to end")]
        public void EnsureUriEndsWithSlash_AppendSlash()
        {
            var input = new Uri("http://1.2.3.4/api");

            var output = input.EnsureUriEndsWithSlash();

            Assert.EndsWith("/", output.ToString());
        }

        [Fact(DisplayName = "EnsureUriEndsWithSlash shall not append slash to end")]
        public void EnsureUriEndsWithSlash_DoesNotAppendSlash()
        {
            var input = new Uri("http://1.2.3.4/api/");

            var output = input.EnsureUriEndsWithSlash();

            Assert.EndsWith("/", output.ToString());
            Assert.False(output.ToString().EndsWith("//"));
        }
    }
}
