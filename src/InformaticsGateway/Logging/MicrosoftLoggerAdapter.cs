// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using Ardalis.GuardClauses;
using FellowOakDicom.Log;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    /// <summary>
    /// Implementation of <see cref="Dicom.Log.Logger"/> for Microsoft.Extensions.Logging.
    /// </summary>
    public class MicrosoftLoggerAdapter : Logger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public MicrosoftLoggerAdapter(Microsoft.Extensions.Logging.ILogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public override void Log(FellowOakDicom.Log.LogLevel level, string msg, params object[] args)
        {
            Guard.Against.NullOrWhiteSpace(msg, nameof(msg));

            _logger.Log(level.ToMicrosoftExtensionsLogLevel(), msg, args);
        }
    }
}
