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

using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Database
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 400, Level = LogLevel.Information, Message = "Configuring database using {databaseType}.")]
        public static partial void UsingDatabaseType(this ILogger logger, string databaseType);

        [LoggerMessage(EventId = 401, Level = LogLevel.Information, Message = "Configuring database for plug-in {plugIn}.")]
        public static partial void ConfigurePlugInDatabase(this ILogger logger, string plugIn);

    }
}
