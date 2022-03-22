// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ConsoleLoggerConfiguration
    {
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
    }

    public static class ConsoleLoggerFactoryExtensions
    {
        public static ILoggingBuilder AddInformaticsGatewayConsole(this ILoggingBuilder builder, Action<ConsoleLoggerConfiguration> configure)
        {
            Guard.Against.Null(configure, nameof(configure));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<ConsoleLoggerConfiguration, ConsoleLoggerProvider>(builder.Services);

            builder.Services.Configure(configure);
            return builder;
        }
    }
}
