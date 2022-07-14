// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public static class Common
    {
        public static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static readonly string MigDirectory = Path.Combine(HomeDir, ".mig");
        public static readonly string ContainerApplicationRootPath = "/opt/monai/ig";
        public static readonly string MountedConfigFilePath = Path.Combine(ContainerApplicationRootPath, "appsettings.json");
        public static readonly string MountedDatabasePath = "/database";
        public static readonly string MountedPlugInsPath = Path.Combine(ContainerApplicationRootPath, "plug-ins");
        public static readonly string ConfigFilePath = Path.Combine(MigDirectory, "appsettings.json");
        public static readonly string AppSettingsResourceName = $"{typeof(Program).Namespace}.Resources.appsettings.json";
    }
}
