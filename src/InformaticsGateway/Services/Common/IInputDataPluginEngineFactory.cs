/*
 * Copyright 2023 MONAI Consortium
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
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    public interface IDataPluginEngineFactory<T>
    {
        IReadOnlyDictionary<string, string> RegisteredPlugins();
    }

    public abstract class DataPluginEngineFactoryBase<T> : IDataPluginEngineFactory<T>
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<DataPluginEngineFactoryBase<T>> _logger;
        private readonly Type _type;

        /// <summary>
        /// A dictionary mapping of input data plug-ins where:
        /// key: <see cref="PluginNameAttribute.Name"/> if available or name of the class.
        /// value: fully qualified assembly type
        /// </summary>
        private readonly Dictionary<string, string> _cachedTypeNames;

        public DataPluginEngineFactoryBase(IFileSystem fileSystem, ILogger<DataPluginEngineFactoryBase<T>> logger)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _type = typeof(T);
            _cachedTypeNames = new Dictionary<string, string>();
        }

        public IReadOnlyDictionary<string, string> RegisteredPlugins()
        {
            LoadAssembliesFromPluginDirectory();

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .Where(p => !p.FullName.Contains("DynamicProxyGenAssembly2"))
                .SelectMany(s => s.GetTypes())
                .Where(p => _type.IsAssignableFrom(p) && p != _type).ToList();

            AddToCache(types);

            return _cachedTypeNames;
        }

        private void AddToCache(List<Type> types)
        {
            Guard.Against.Null(types, nameof(types));

            if (types.Any())
            {
                types.ForEach(p =>
                {
                    if (!_cachedTypeNames.ContainsValue(p.GetShortTypeAssemblyName()))
                    {
                        var nameAttribute = p.GetCustomAttribute<PluginNameAttribute>();

                        var name = nameAttribute is null ? p.Name : nameAttribute.Name;
                        _cachedTypeNames.Add(name, p.GetShortTypeAssemblyName());
                        _logger.DataPluginFound(_type.Name, name, p.GetShortTypeAssemblyName());
                    }
                });
            }
        }

        private void LoadAssembliesFromPluginDirectory()
        {
            var files = _fileSystem.Directory.GetFiles(SR.PlugInDirectoryPath, "*.dll", System.IO.SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                _logger.LoadingAssembly(file);
                var assembly = Assembly.LoadFile(file);
                var matchingTypes = assembly.GetTypes().Where(p => _type.IsAssignableFrom(p) && p != _type).ToList();
                AddToCache(matchingTypes);
            }
        }
    }

    public class InputDataPluginEngineFactory : DataPluginEngineFactoryBase<IInputDataPlugin>
    {
        public InputDataPluginEngineFactory(IFileSystem fileSystem, ILogger<DataPluginEngineFactoryBase<IInputDataPlugin>> logger) : base(fileSystem, logger)
        {
        }
    }

    public class OutputDataPluginEngineFactory : DataPluginEngineFactoryBase<IOutputDataPlugin>
    {
        public OutputDataPluginEngineFactory(IFileSystem fileSystem, ILogger<DataPluginEngineFactoryBase<IOutputDataPlugin>> logger) : base(fileSystem, logger)
        {
        }
    }
}
