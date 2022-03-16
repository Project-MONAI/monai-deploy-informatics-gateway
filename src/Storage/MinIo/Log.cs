// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Storage
{
    public static partial class Log
    {

        [LoggerMessage(EventId = 20000, Level = LogLevel.Error, Message = "Error listing objects in bucket '{bucketName}'.")]
        public static partial void ListObjectError(this ILogger logger, string bucketName);
    }
}
