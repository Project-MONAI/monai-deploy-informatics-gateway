// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

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
