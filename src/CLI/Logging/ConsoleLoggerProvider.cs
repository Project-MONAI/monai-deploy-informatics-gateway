// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers;
        private readonly ConsoleLoggerConfiguration _configuration;

        public ConsoleLoggerProvider(IOptions<ConsoleLoggerConfiguration> configuration)
        {
            _loggers = new ConcurrentDictionary<string, ConsoleLogger>();
            _configuration = configuration.Value;
        }

        public ILogger CreateLogger(string categoryName)
            => _loggers.GetOrAdd(categoryName, name => new ConsoleLogger(name, _configuration));

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
