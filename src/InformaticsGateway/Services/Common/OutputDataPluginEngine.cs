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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal class OutputDataPluginEngine : IOutputDataPluginEngine
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutputDataPluginEngine> _logger;
        private readonly IDicomToolkit _dicomToolkit;
        private IReadOnlyList<IOutputDataPlugin> _plugsins;

        public OutputDataPluginEngine(IServiceProvider serviceProvider, ILogger<OutputDataPluginEngine> logger, IDicomToolkit dicomToolkit)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
        }

        public void Configure(IReadOnlyList<string> pluginAssemblies)
        {
            _plugsins = LoadPlugins(_serviceProvider, pluginAssemblies);
        }

        public async Task<ExportRequestDataMessage> ExecutePlugins(ExportRequestDataMessage exportRequestDataMessage)
        {
            if (_plugsins == null)
            {
                throw new ApplicationException("InputDataPluginEngine not configured, please call Configure() first.");
            }

            var dicomFile = _dicomToolkit.Load(exportRequestDataMessage.FileContent);
            foreach (var plugin in _plugsins)
            {
                _logger.ExecutingOutputDataPlugin(plugin.Name);
                (dicomFile, exportRequestDataMessage) = await plugin.Execute(dicomFile, exportRequestDataMessage).ConfigureAwait(false);
            }
            using var ms = new MemoryStream();
            await dicomFile.SaveAsync(ms);
            exportRequestDataMessage.SetData(ms.ToArray());

            return exportRequestDataMessage;
        }

        private IReadOnlyList<IOutputDataPlugin> LoadPlugins(IServiceProvider serviceProvider, IReadOnlyList<string> pluginAssemblies)
        {
            var exceptions = new List<Exception>();
            var list = new List<IOutputDataPlugin>();
            foreach (var plugin in pluginAssemblies)
            {
                try
                {
                    _logger.AddingOutputDataPlugin(plugin);
                    list.Add(typeof(IOutputDataPlugin).CreateInstance<IOutputDataPlugin>(serviceProvider, typeString: plugin));
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
