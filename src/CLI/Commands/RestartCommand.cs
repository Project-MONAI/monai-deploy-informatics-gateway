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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class RestartCommand : Command
    {
        public RestartCommand() : base("restart", "Restart the MONAI Informatics Gateway service")
        {
            this.Handler = CommandHandler.Create<IHost, bool>(RestartCommandHandler);
        }

        private static async Task RestartCommandHandler(IHost host, bool verbose)
        {
            var service = host.Services.GetRequiredService<IControlService>();
            await service.Restart();
        }
    }
}