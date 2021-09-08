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
