/*
 * Copyright 2022 MONAI Consortium
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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework
{
    public class EfDatabaseMigrationManager : IDatabaseMigrationManager
    {
        public IHost Migrate(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                using (var dbContext = scope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>())
                {
                    try
                    {
                        dbContext.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetService<ILogger>();
                        logger?.Log(LogLevel.Critical, "Failed to migrate database", ex);
                        throw;
                    }
                }
            }
            return host;
        }
    }
}
