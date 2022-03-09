// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.CommandLine.Rendering;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface IConsoleRegion
    {
        Region GetDefaultConsoleRegion();
    }

    internal class ConsoleRegion : IConsoleRegion
    {
        public Region GetDefaultConsoleRegion()
        {
            return new Region(0, 0, Console.WindowWidth, Console.WindowHeight, false);
        }
    }
}
