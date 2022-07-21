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

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Common.Test
{
    public class ExtensionMethodsTest
    {
        [Fact(DisplayName = "IsNullOrEmpty shall return true for null input")]
        public void IsNullOrEmpty_WithNullInput()
        {
            List<string> list = null;
            Assert.True(list.IsNullOrEmpty());
        }

        [Fact(DisplayName = "IsNullOrEmpty shall return true for empty list")]
        public void IsNullOrEmpty_WithEmptyList()
        {
            var list = new List<string>();
            Assert.True(list.IsNullOrEmpty());
        }

        [Fact(DisplayName = "IsNullOrEmpty to use Any with non ICollection")]
        public void IsNullOrEmpty_UseAnyForNonICollection()
        {
            var stack = new Stack<int>();
            stack.Push(1);
            Assert.False(stack.IsNullOrEmpty());
        }

        [Theory(DisplayName = "IsNullOrEmpty shall return false with at least one items")]
        [InlineData("go")]
        [InlineData("team", "monai")]
        [InlineData("team", "monai", "rocks")]
        public void IsNullOrEmpty_WithOneItemInList(params string[] items)
        {
            var list = new List<string>(items);
            Assert.False(ExtensionMethods.IsNullOrEmpty(list));
        }

        [Theory(DisplayName = "RemoveInvalidChars shall return original input")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void RemoveInvalidChars_HandleNullInput(string input)
        {
            Assert.Equal(input, ExtensionMethods.RemoveInvalidPathChars(input));
        }

        [Fact(DisplayName = "RemoveInvalidChars shall remove invalid path characters")]
        public void RemoveInvalidChars_ShallRemoveInvalidPathCharacters()
        {
            var invalidChars = string.Join("", Path.GetInvalidPathChars());
            var input = "team" + invalidChars + "monai";

            Assert.Equal("teammonai", input.RemoveInvalidPathChars());
        }
    }
}
