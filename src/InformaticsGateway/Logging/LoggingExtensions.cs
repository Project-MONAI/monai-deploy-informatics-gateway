// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static class LoggingExtensions
    {
        public static Microsoft.Extensions.Logging.LogLevel ToMicrosoftExtensionsLogLevel(this FellowOakDicom.Log.LogLevel dicomLogLevel)
        {
            return dicomLogLevel switch
            {
                FellowOakDicom.Log.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
                FellowOakDicom.Log.LogLevel.Fatal => Microsoft.Extensions.Logging.LogLevel.Critical,
                FellowOakDicom.Log.LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
                FellowOakDicom.Log.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
                _ => Microsoft.Extensions.Logging.LogLevel.Debug
            };
        }
    }
}
