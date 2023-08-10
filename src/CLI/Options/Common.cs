/*
 * Copyright 2021-2022 MONAI Consortium
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
        public static readonly string MountedNLogConfigFilePath = Path.Combine(ContainerApplicationRootPath, "nlog.config");
        public static readonly string MountedDatabasePath = "/database";
        public static readonly string MountedPlugInsPath = Path.Combine(ContainerApplicationRootPath, "plug-ins");
        public static readonly string ConfigFilePath = Path.Combine(MigDirectory, "appsettings.json");
        public static readonly string NLogConfigFilePath = Path.Combine(MigDirectory, "nlog.config");
        public static readonly string AppSettingsResourceName = $"{typeof(Program).Namespace}.Resources.appsettings.json";
        public static readonly string NLogConfigResourceName = $"{typeof(Program).Namespace}.Resources.nlog.config";
    }
}
