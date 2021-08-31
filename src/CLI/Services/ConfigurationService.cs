// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public interface IConfigurationService
    {
        void CreateConfigDirectoryIfNotExist();

        bool ConfigurationExists();

        ConfigurationOptions Load();

        ConfigurationOptions Load(bool verbose);

        void Save(ConfigurationOptions options);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void CreateConfigDirectoryIfNotExist()
        {
            if (!Directory.Exists(Common.MigDirectory))
            {
                Directory.CreateDirectory(Common.MigDirectory);
            }
        }

        public bool ConfigurationExists()
        {
            return File.Exists(Common.CliConfigFilePath);
        }

        public ConfigurationOptions Load() => Load(false);

        public ConfigurationOptions Load(bool verbose)
        {
            try
            {
                if (verbose)
                {
                    this._logger.Log(LogLevel.Debug, "Loading configuration file from {0}", Common.CliConfigFilePath);
                }

                using (var file = File.OpenText(Common.CliConfigFilePath))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize(file, typeof(ConfigurationOptions)) as ConfigurationOptions;
                }
            }
            catch (Exception)
            {
                this._logger.Log(LogLevel.Warning, "Existing configuration file may be corrupted, createing a new one.");
                return new ConfigurationOptions();
            }
        }

        public void Save(ConfigurationOptions options)
        {
            using (var file = File.CreateText(Common.CliConfigFilePath))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, options);
            }

            this._logger.Log(LogLevel.Information, $"Configuration file {Common.CliConfigFilePath} updated successfully.");
        }
    }
}