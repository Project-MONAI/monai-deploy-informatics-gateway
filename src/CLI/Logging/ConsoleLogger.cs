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

using System;
using Ardalis.GuardClauses;
using Crayon;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ConsoleLogger : ILogger
    {
        private readonly ConsoleLoggerConfiguration _configuration;

        public ConsoleLogger(string name, ConsoleLoggerConfiguration configuration)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));

            _ = name;
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

            var message = formatter(state, exception);

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
