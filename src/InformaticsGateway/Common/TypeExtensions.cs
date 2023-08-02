/*
 * Copyright 2022-2023 MONAI Consortium
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
using System.IO;
using System.Linq;
using System.Reflection;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public static class TypeExtensions
    {
        public static T CreateInstance<T>(this Type type, IServiceProvider serviceProvider, params object[] parameters)
        {
            Guard.Against.Null(type, nameof(type));
            Guard.Against.Null(serviceProvider, nameof(serviceProvider));

            return (T)ActivatorUtilities.CreateInstance(serviceProvider, type, parameters);
        }

        public static T CreateInstance<T>(this Type interfaceType, IServiceProvider serviceProvider, string typeString, params object[] parameters)
        {
            Guard.Against.Null(interfaceType, nameof(interfaceType));
            Guard.Against.Null(serviceProvider, nameof(serviceProvider));
            Guard.Against.NullOrWhiteSpace(typeString, nameof(typeString));

            var type = interfaceType.GetType(typeString);
            var processor = ActivatorUtilities.CreateInstance(serviceProvider, type, parameters);

            return (T)processor;
        }

        public static Type GetType(this Type interfaceType, string typeString)
        {
            Guard.Against.Null(interfaceType, nameof(interfaceType));
            Guard.Against.NullOrWhiteSpace(typeString, nameof(typeString));

            var type = Type.GetType(
                      typeString,
                      (name) =>
                      {
                          var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(z => !string.IsNullOrWhiteSpace(z.FullName) && z.FullName.StartsWith(name.FullName));

                          assembly ??= Assembly.LoadFile(Path.Combine(SR.PlugInDirectoryPath, $"{name.Name}.dll"));

                          return assembly;
                      },
                      null,
                      true);

            if (type is not null &&
                (type.IsSubclassOf(interfaceType) ||
                (type.BaseType is not null && type.BaseType.IsAssignableTo(interfaceType)) ||
                (type.GetInterfaces().Contains(interfaceType))))
            {
                return type;
            }

            throw new NotSupportedException($"{typeString} is not a sub-type of {interfaceType.Name}");
        }
    }
}
