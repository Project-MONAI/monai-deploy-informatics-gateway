// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO;
using Crayon;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ConsoleLoggerTest
    {
        [Fact(DisplayName = "BeginScope is not supported")]
        public void BeingScope_IsNotSupported()
        {
            var logger = new ConsoleLogger("test", new ConsoleLoggerConfiguration
            {
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information
            });

            var data = "test";
            Assert.Null(logger.BeginScope(data));
        }

        [Fact(DisplayName = "IsEnabled")]
        public void IsEnabled()
        {
            var logger = new ConsoleLogger("test", new ConsoleLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information
            });

            Assert.False(logger.IsEnabled(LogLevel.Trace));
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.True(logger.IsEnabled(LogLevel.Critical));
        }

        [Fact(DisplayName = "Log ignored")]
        public void Log_Ignored()
        {
            var logger = new ConsoleLogger("test", new ConsoleLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Error
            });
            using (var sw = new StringWriter())
            {
                var message = "Hello";
                Console.SetOut(sw);
                logger.Log(LogLevel.Trace, message);
                var result = sw.ToString();
                Assert.Equal(string.Empty, result);
            }
        }

        [Fact(DisplayName = "Log trace")]
        public void Log_Trace()
        {
            var logger = new ConsoleLogger("test", new ConsoleLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Trace
            });
            using (var sw = new StringWriter())
            {
                var message = "Hello";
                Console.SetOut(sw);
                logger.Log(LogLevel.Trace, message);
                var result = sw.ToString();
                Assert.Equal($"{Output.Bright.Black("trce: ")}{message}{Environment.NewLine}", result);
            }
        }

        [Fact(DisplayName = "Log debug")]
        public void Log_Debug()
        {
            var logger = new ConsoleLogger("test", new ConsoleLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Trace
            });
            using (var sw = new StringWriter())
            {
                var message = "Hello";
                Console.SetOut(sw);
                logger.Log(LogLevel.Debug, message);
                var result = sw.ToString();
                Assert.Equal($"{Output.Bright.Black("dbug: ")}{message}{Environment.NewLine}", result);
            }
        }

        [Fact(DisplayName = "Log information")]
        public void Log_Information()
        {
            var logger = new ConsoleLogger("test", new ConsoleLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Trace
            });
            using (var sw = new StringWriter())
            {
                var message = "Hello";
                Console.SetOut(sw);
                logger.Log(LogLevel.Information, message);
                var result = sw.ToString();
                Assert.Equal($"{Output.Bright.White("info: ")}{message}{Environment.NewLine}", result);
            }
        }

        [Fact(DisplayName = "Log warning")]
        public void Log_Warning()
        {
            var logger = new ConsoleLogger("test", new ConsoleLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Warning
            });
            using (var sw = new StringWriter())
            {
                var message = "Hello";
                Console.SetOut(sw);
                Console.SetError(sw);
                logger.Log(LogLevel.Warning, message);
                sw.Flush();
                var result = sw.ToString();
                Assert.Equal($"{Output.Bright.Yellow("warn: ")}{Output.Bright.Yellow(message)}{Environment.NewLine}", result);
            }
        }

        [Fact(DisplayName = "Log with exception")]
        public void Log_WithException()
        {
            var logger = new ConsoleLogger("test", new ConsoleLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Error
            });
            using (var sw = new StringWriter())
            {
                var exception = new Exception("error");
                var message = "Hello";
                Console.SetOut(sw);
                Console.SetError(sw);
                logger.Log(LogLevel.Error, exception, message);
                var result = sw.ToString();
                Assert.Equal($"{Output.Bright.Red("fail: ")}{Output.Bright.Red(message + Environment.NewLine + exception.ToString())}{Environment.NewLine}", result);
            }
        }

        [Fact(DisplayName = "Log Critical")]
        public void Log_Critical()
        {
            var logger = new ConsoleLogger("test", new ConsoleLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Warning
            });
            using (var sw = new StringWriter())
            {
                var message = "Hello";
                Console.SetOut(sw);
                Console.SetError(sw);
                logger.Log(LogLevel.Critical, message);
                sw.Flush();
                var result = sw.ToString();
                Assert.Equal($"{Output.Bright.Red("crit: ")}{Output.Bright.Red(message)}{Environment.NewLine}", result);
            }
        }
    }
}
