// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Ardalis.GuardClauses;
using Crayon;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ConsoleLogger : ILogger
    {
        private readonly string _name;
        private readonly ConsoleLoggerConfiguration _configuration;

        public ConsoleLogger(string name, ConsoleLoggerConfiguration configuration)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));

            _name = name;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => _configuration.MinimumLogLevel <= logLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception);

            if (exception != null)
            {
                message += $"{Environment.NewLine}{exception}";
            }

            switch (logLevel)
            {
                case LogLevel.Trace:
                    Console.Out.WriteAsync(Output.Bright.Black("trce: "));
                    Console.Out.WriteLineAsync(message);
                    break;

                case LogLevel.Debug:
                    Console.Out.WriteAsync(Output.Bright.Black("dbug: "));
                    Console.Out.WriteLineAsync(message);
                    break;

                case LogLevel.Information:
                    Console.Out.WriteAsync(Output.Bright.White("info: "));
                    Console.Out.WriteLineAsync(message);
                    break;

                case LogLevel.Warning:
                    Console.Out.WriteAsync(Output.Bright.Yellow("warn: "));
                    Console.Error.WriteLineAsync(Output.Bright.Yellow(message));
                    break;

                case LogLevel.Error:
                    Console.Error.WriteAsync(Output.Bright.Red("fail: "));
                    Console.Error.WriteLineAsync(Output.Bright.Red(message));
                    break;

                case LogLevel.Critical:
                    Console.Error.WriteAsync(Output.Bright.Red("crit: "));
                    Console.Error.WriteLineAsync(Output.Bright.Red(message));
                    break;
            }
        }
    }
}
