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
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal class InputDataPluginEngine : IInputDataPluginEngine
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InputDataPluginEngine> _logger;
        private IReadOnlyList<IInputDataPlugin> _plugsins;

        public InputDataPluginEngine(IServiceProvider serviceProvider, ILogger<InputDataPluginEngine> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Configure(IReadOnlyList<string> pluginAssemblies)
        {
            _plugsins = LoadPlugins(_serviceProvider, pluginAssemblies);
        }

        public async Task<Tuple<DicomFile, FileStorageMetadata>> ExecutePlugins(DicomFile dicomFile, FileStorageMetadata fileMetadata)
        {
            if (_plugsins == null)
            {
                throw new ApplicationException("InputDataPluginEngine not configured, please call Configure() first.");
            }

            foreach (var plugin in _plugsins)
            {
                _logger.ExecutingInputDataPlugin(plugin.Name);
                (dicomFile, fileMetadata) = await plugin.Execute(dicomFile, fileMetadata).ConfigureAwait(false);
            }

            return new Tuple<DicomFile, FileStorageMetadata>(dicomFile, fileMetadata);
        }

        private IReadOnlyList<IInputDataPlugin> LoadPlugins(IServiceProvider serviceProvider, IReadOnlyList<string> pluginAssemblies)
        {
            var exceptions = new List<Exception>();
            var list = new List<IInputDataPlugin>();
            foreach (var plugin in pluginAssemblies)
            {
                try
                {
                    _logger.AddingInputDataPlugin(plugin);
                    list.Add(typeof(IInputDataPlugin).CreateInstance<IInputDataPlugin>(serviceProvider, typeString: plugin));
                }
                catch (Exception ex)
                {
                    exceptions.Add(new PlugingLoadingException($"Error loading plug-in '{plugin}'.", ex));
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException("Error loading plug-in(s).", exceptions);
            }

            return list;
        }
    }
}
