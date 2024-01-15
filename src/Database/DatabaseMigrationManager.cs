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
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api;

namespace Monai.Deploy.InformaticsGateway.Database
{
    public static class DatabaseMigrationManager
    {
        public static IHost MigrateDatabase(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<IDatabaseMigrationManager>()?.Migrate(host);
                var fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystem>();

                host.MigrateDatabaseFromExternalPlugIns(fileSystem);
            }
            return host;
        }

        private static IHost MigrateDatabaseFromExternalPlugIns(this IHost host, IFileSystem fileSystem)
        {
            var assemblies = DatabaseManager.LoadAssemblyFromPlugInsDirectory(fileSystem);
            var matchingTypes = FindMatchingTypesFromAssemblies(assemblies);

            foreach (var type in matchingTypes)
            {
                if (Activator.CreateInstance(type) is not IDatabaseMigrationManager migrationManager)
                {
                    throw new ConfigurationException($"Error activating IDatabaseMigrationManager from type '{type.FullName}'.");
                }
                migrationManager.Migrate(host);
            }
            return host;
        }

        private static Type[] FindMatchingTypesFromAssemblies(Assembly[] assemblies)
        {
            var matchingTypes = new List<Type>();
            foreach (var assembly in assemblies)
            {
                var types = assembly.ExportedTypes.Where(p => p.IsAssignableFrom(typeof(IDatabaseMigrationManager)) && p.Name != nameof(IDatabaseMigrationManager));
                if (types.Any())
                {
                    matchingTypes.AddRange(types);
                }
            }

            return matchingTypes.ToArray();
        }
    }
}
