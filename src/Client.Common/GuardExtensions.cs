/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2020 NVIDIA Corporation
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
    public static class GuardExtensions
    {
        public static void MalformUri(this IGuardClause guardClause, Uri input, string parameterName)
        {
            Guard.Against.Null(guardClause, parameterName);
            Guard.Against.Null(input, parameterName);

            if (!input.IsWellFormedOriginalString())
            {
                throw new ArgumentException("uri not well formed", parameterName);
            }

            if (input.Scheme != "http" && input.Scheme != "https")
            {
                throw new ArgumentException("invalid scheme in uri", parameterName);
            }
        }

        public static void MalformUri(this IGuardClause guardClause, string input, string parameterName)
        {
            Guard.Against.Null(guardClause, parameterName);
            Guard.Against.NullOrWhiteSpace(input, parameterName);
            Guard.Against.MalformUri(new Uri(input), parameterName);
        }

        public static void OutOfRangePort(this IGuardClause guardClause, int port, string parameterName)
        {
            Guard.Against.Null(guardClause, parameterName);
            if (port <= 0 || port > 65535)
            {
                throw new ArgumentException("invalid port number", parameterName);
            }
        }
    }
}
