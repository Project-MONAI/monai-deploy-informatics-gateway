// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.Client.Common
{
    public static class UriExtensions
    {
        public static Uri EnsureUriEndsWithSlash(this Uri input)
        {
            Guard.Against.MalformUri(input, nameof(input));

            var str = input.ToString();

            if (!string.IsNullOrWhiteSpace(str) && !str.EndsWith("/"))
            {
#pragma warning disable S1075 // URIs should not be hardcoded
                return new Uri(str + '/');
#pragma warning restore S1075 // URIs should not be hardcoded
            }

            return input;
        }
    }
}
