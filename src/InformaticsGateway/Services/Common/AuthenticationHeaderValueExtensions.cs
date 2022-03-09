// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    public static class AuthenticationHeaderValueExtensions
    {
        public static AuthenticationHeaderValue ConvertFrom(ConnectionAuthType connectionAuthType, string authId)
        {
            return connectionAuthType switch
            {
                ConnectionAuthType.Basic => new AuthenticationHeaderValue("Basic", authId),
                ConnectionAuthType.Bearer => new AuthenticationHeaderValue("Bearer", authId),
                ConnectionAuthType.None => null,
                _ => throw new InferenceRequestException($"Unsupported ConnectionAuthType: {connectionAuthType}"),
            };
        }
    }
}
