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
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers;
        private readonly ConsoleLoggerConfiguration _configuration;

        public ConsoleLoggerProvider(IOptions<ConsoleLoggerConfiguration> configuration)
        {
            _loggers = new ConcurrentDictionary<string, ConsoleLogger>();
            _configuration = configuration.Value;
        }

        public ILogger CreateLogger(string categoryName)
            => _loggers.GetOrAdd(categoryName, name => new ConsoleLogger(name, _configuration));

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}