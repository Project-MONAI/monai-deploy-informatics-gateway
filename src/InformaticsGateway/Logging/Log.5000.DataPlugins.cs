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

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 5000, Level = LogLevel.Information, Message = "Loading assembly from {filename}.")]
        public static partial void LoadingAssembly(this ILogger logger, string filename);

        [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "{type} data plug-in found {name}: {plugin}.")]
        public static partial void DataPlugInFound(this ILogger logger, string type, string name, string plugin);

        [LoggerMessage(EventId = 5002, Level = LogLevel.Debug, Message = "Adding input data plug-in: {plugin}.")]
        public static partial void AddingInputDataPlugIn(this ILogger logger, string plugin);

        [LoggerMessage(EventId = 5003, Level = LogLevel.Information, Message = "Executing input data plug-in: {plugin}.")]
        public static partial void ExecutingInputDataPlugIn(this ILogger logger, string plugin);

        [LoggerMessage(EventId = 5004, Level = LogLevel.Debug, Message = "Adding output data plug-in: {plugin}.")]
        public static partial void AddingOutputDataPlugIn(this ILogger logger, string plugin);

        [LoggerMessage(EventId = 5005, Level = LogLevel.Information, Message = "Executing output data plug-in: {plugin}.")]
        public static partial void ExecutingOutputDataPlugIn(this ILogger logger, string plugin);

        [LoggerMessage(EventId = 5006, Level = LogLevel.Debug, Message = "Adding SCP Listener {serviceName} on port {port}")]
        public static partial void AddingScpListener(this ILogger logger, string serviceName, int port);
    }
}
