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
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;


namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal class OutputDataPlugInEngine : IOutputDataPlugInEngine
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutputDataPlugInEngine> _logger;
        private readonly IDicomToolkit _dicomToolkit;
        private IReadOnlyList<IOutputDataPlugIn>? _plugsins;

        public OutputDataPlugInEngine(IServiceProvider serviceProvider, ILogger<OutputDataPlugInEngine> logger, IDicomToolkit dicomToolkit)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
        }

        public void Configure(IReadOnlyList<string> pluginAssemblies)
        {
            _plugsins = LoadPlugIns(_serviceProvider, pluginAssemblies);
        }

        public async Task<ExportRequestDataMessage> ExecutePlugInsAsync(ExportRequestDataMessage exportRequestDataMessage)
        {
            if (_plugsins == null)
            {
                throw new PlugInInitializationException("InputDataPlugInEngine not configured, please call Configure() first.");
            }

            var dicomFile = _dicomToolkit.Load(exportRequestDataMessage.FileContent);
            foreach (var plugin in _plugsins)
            {
                _logger.ExecutingOutputDataPlugIn(plugin.Name);
                (dicomFile, exportRequestDataMessage) = await plugin.ExecuteAsync(dicomFile, exportRequestDataMessage).ConfigureAwait(false);
            }
            using var ms = new MemoryStream();
            await dicomFile.SaveAsync(ms);
            exportRequestDataMessage.SetData(ms.ToArray());

            return exportRequestDataMessage;
        }

        private IReadOnlyList<IOutputDataPlugIn> LoadPlugIns(IServiceProvider serviceProvider, IReadOnlyList<string> pluginAssemblies)
        {
            var exceptions = new List<Exception>();
            var list = new List<IOutputDataPlugIn>();
            foreach (var plugin in pluginAssemblies)
            {
                try
                {
                    _logger.AddingOutputDataPlugIn(plugin);
                    list.Add(typeof(IOutputDataPlugIn).CreateInstance<IOutputDataPlugIn>(serviceProvider, typeString: plugin));
                }
                catch (Exception ex)
                {
                    _logger.ErrorAddingOutputDataPlugIn(ex, plugin);
                    exceptions.Add(new PlugInLoadingException($"Error loading plug-in '{plugin}'.", ex));
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
