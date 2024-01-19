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
using HL7.Dotnetcore;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    public class InputHL7DataPlugInEngine : IInputHL7DataPlugInEngine
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InputHL7DataPlugInEngine> _logger;
        private IReadOnlyList<IInputHL7DataPlugIn>? _plugsins;

        public InputHL7DataPlugInEngine(IServiceProvider serviceProvider, ILogger<InputHL7DataPlugInEngine> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Configure(IReadOnlyList<string> pluginAssemblies)
        {
            _plugsins = LoadPlugIns(_serviceProvider, pluginAssemblies);
        }

        public async Task<Tuple<Message, FileStorageMetadata>> ExecutePlugInsAsync(Message hl7File, FileStorageMetadata fileMetadata, Hl7ApplicationConfigEntity? configItem)
        {
            if (configItem?.PlugInAssemblies is not null && configItem.PlugInAssemblies.Any())
            {
                if (_plugsins == null)
                {
                    throw new PlugInInitializationException("InputHL7DataPlugInEngine not configured, please call Configure() first.");
                }

                foreach (var plugin in _plugsins)
                {
                    if (configItem is not null && configItem.PlugInAssemblies.Exists(a => a.StartsWith(plugin.ToString()!)))
                    {
                        _logger.ExecutingInputDataPlugIn(plugin.Name);
                        (hl7File, fileMetadata) = await plugin.ExecuteAsync(hl7File, fileMetadata).ConfigureAwait(false);
                    }
                }
            }
            return new Tuple<Message, FileStorageMetadata>(hl7File, fileMetadata);
        }

        private IReadOnlyList<IInputHL7DataPlugIn> LoadPlugIns(IServiceProvider serviceProvider, IReadOnlyList<string> pluginAssemblies)
        {
            var exceptions = new List<Exception>();
            var list = new List<IInputHL7DataPlugIn>();
            foreach (var plugin in pluginAssemblies)
            {
                try
                {
                    _logger.AddingInputDataPlugIn(plugin);
                    list.Add(typeof(IInputHL7DataPlugIn).CreateInstance<IInputHL7DataPlugIn>(serviceProvider, typeString: plugin));
                }
                catch (Exception ex)
                {
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
