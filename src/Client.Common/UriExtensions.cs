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
                return new Uri(str + '/');
            }

            return input;
        }
    }
}
