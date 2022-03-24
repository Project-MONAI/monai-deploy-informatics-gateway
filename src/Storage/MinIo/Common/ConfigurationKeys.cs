// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.Storage.Common
{
    internal static class ConfigurationKeys
    {
        public static readonly string EndPoint = "endpoint";
        public static readonly string AccessKey = "accessKey";
        public static readonly string AccessToken = "accessToken";
        public static readonly string SecuredConnection = "securedConnection";

        public static readonly string[] RequiredKeys = new[] { EndPoint, AccessKey, AccessToken, SecuredConnection };
    }
}
