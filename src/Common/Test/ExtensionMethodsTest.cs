// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
