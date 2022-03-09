// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Common
{
    internal static class IServiceProviderExtensions
    {
        internal static TInterface LocateService<TInterface>(this IServiceProvider serviceProvider, ILogger<Program> logger, string fullyQualifiedTypeString)
        {
            var type = Type.GetType(fullyQualifiedTypeString);
            if (type is null)
            {
                logger.Log(LogLevel.Critical, $"Type '{fullyQualifiedTypeString}' cannot be found.");
                throw new ConfigurationException($"Type '{fullyQualifiedTypeString}' cannot be found.");
            }

            var instance = serviceProvider.GetService(type);
            if (instance is null)
            {
                logger.Log(LogLevel.Critical, $"Instance of '{fullyQualifiedTypeString}' cannot be found.");
                throw new ConfigurationException($"Instance of '{fullyQualifiedTypeString}' cannot be found.");
            }

            return (TInterface)instance;
        }
    }
}
