/*
 * Apache License, Version 2.0
 * Copyright 2019-2020 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ardalis.GuardClauses;
using System;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Client.Common.Test
{
    public class GuardExtensionsTest
    {
        [Fact(DisplayName = "MalformUri shall throw when input is null")]
        public void MalformUri_Null()
        {
            Uri input = null;

            Assert.Throws<ArgumentNullException>(() => Guard.Against.MalformUri(input, nameof(input)));
        }

        [Fact(DisplayName = "MalformUri shall throw with malformed input")]
        public void MalformUri_MalformedInput()
        {
            Uri input = new Uri("http://www.contoso.com/path???/file name");

            Assert.Throws<ArgumentException>(() => Guard.Against.MalformUri(input, nameof(input)));
        }

        [Fact(DisplayName = "MalformUri shall throw if not http/https")]
        public void MalformUri_NoneHttpHttps()
        {
            Uri input = new Uri("ftp://www.contoso.com/api/123");

            Assert.Throws<ArgumentException>(() => Guard.Against.MalformUri(input, nameof(input)));
        }

        [Fact(DisplayName = "MalformUri shall pass")]
        public void MalformUri_Valid()
        {
            string input = "http://www.contoso.com/api/123";
            Guard.Against.MalformUri(input, nameof(input));

            Uri input2 = new Uri("https://www.contoso.com/api/123");
            Guard.Against.MalformUri(input, nameof(input2));
        }

        [Fact(DisplayName = "OutOfRangePort")]
        public void OutOfRangePort()
        {
            int input = 100;
            Guard.Against.OutOfRangePort(input, nameof(input));
            input = 65535;
            Guard.Against.OutOfRangePort(input, nameof(input));

            input = 0;
            Assert.Throws<ArgumentException>(() => Guard.Against.OutOfRangePort(input, nameof(input)));
            input = 65536;
            Assert.Throws<ArgumentException>(() => Guard.Against.OutOfRangePort(input, nameof(input)));
        }
    }
}
