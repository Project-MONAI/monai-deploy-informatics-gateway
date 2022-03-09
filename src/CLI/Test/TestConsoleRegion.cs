// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.CommandLine.Rendering;
using Monai.Deploy.InformaticsGateway.CLI.Services;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    internal class TestConsoleRegion : IConsoleRegion
    {
        public Region GetDefaultConsoleRegion()
        {
            return new Region(0, 0, 100, 100, false);
        }
    }
}
