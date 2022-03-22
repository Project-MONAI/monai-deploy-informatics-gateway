// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.Common;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.DicomWebClient.Test
{
    public class ExtensionTest
    {
        [Fact(DisplayName = "Trim - Null input returns null")]
        public void Trim_NullInputReturnsNull()
        {
            int[] input = null;
            Assert.Null(input.Trim());
        }

        [Fact(DisplayName = "Trim -  shall remove null items")]
        public void Trim_RemovesNullItems()
        {
            var input = new int?[] { 1, 2, null };

            var output = input.Trim();

            Assert.Equal(2, output.Length);
            Assert.Contains(1, output);
            Assert.Contains(2, output);
        }
    }
}
