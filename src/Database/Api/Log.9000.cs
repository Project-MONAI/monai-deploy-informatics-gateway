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

namespace Monai.Deploy.InformaticsGateway.Database.Api
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 9000, Level = LogLevel.Error, Message = "Error adding item {type} to the database.")]
        public static partial void ErrorAddItem(this ILogger logger, string @type, Exception ex);

        [LoggerMessage(EventId = 9001, Level = LogLevel.Error, Message = "Error updating item {type} in the database.")]
        public static partial void ErrorUpdateItem(this ILogger logger, string @type, Exception ex);

        [LoggerMessage(EventId = 9002, Level = LogLevel.Error, Message = "Error deleting item {type} from the database.")]
        public static partial void ErrorDeleteItem(this ILogger logger, string @type, Exception ex);
    }
}
