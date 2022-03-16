// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Common
{
    internal static class IServiceProviderExtensions
    {
        internal static TInterface LocateService<TInterface>(this IServiceProvider serviceProvider, ILogger<Program> logger, string fullyQualifiedTypeString)
        {
            var type = Type.GetType(fullyQualifiedTypeString);
            if (type is null)
            {
                logger.TypeNotFound(fullyQualifiedTypeString);
                throw new ConfigurationException($"Type '{fullyQualifiedTypeString}' cannot be found.");
            }

            var instance = serviceProvider.GetService(type);
            if (instance is null)
            {
                logger.InstanceOfTypeNotFound(fullyQualifiedTypeString);
                throw new ConfigurationException($"Instance of '{fullyQualifiedTypeString}' cannot be found.");
            }

            return (TInterface)instance;
        }
    }
}
