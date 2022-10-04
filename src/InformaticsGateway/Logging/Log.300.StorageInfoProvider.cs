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

using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        // StorageInfoProvider
        [LoggerMessage(EventId = 300, Level = LogLevel.Information, Message = "Temporary Storage Path={path}. Storage Size: {totalSize:N0}. Reserved: {reservedSpace:N0}.")]
        public static partial void StorageInfoProviderStartup(this ILogger logger, string path, long totalSize, long reservedSpace);

        [LoggerMessage(EventId = 301, Level = LogLevel.Information, Message = "Storage Size: {totalSize:N0}. Reserved: {reservedSpace:N0}. Available: {freeSpace:N0}.")]
        public static partial void CurrentStorageSize(this ILogger logger, long totalSize, long reservedSpace, long freeSpace);
    }
}
