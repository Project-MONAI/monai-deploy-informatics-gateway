/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2020 NVIDIA Corporation
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
