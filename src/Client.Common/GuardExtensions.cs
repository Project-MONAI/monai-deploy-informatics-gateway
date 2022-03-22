// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

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
