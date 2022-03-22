// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Ardalis.GuardClauses;
using FellowOakDicom.Log;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public class FoDicomLogManager : LogManager
    {
        private readonly ILoggerFactory _loggerFactory;

        public FoDicomLogManager(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));
        }

        protected override Logger GetLoggerImpl(string name)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));

            return new MicrosoftLoggerAdapter(_loggerFactory.CreateLogger(name));
        }
    }
}
