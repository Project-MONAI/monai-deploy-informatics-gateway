// SPDX-FileCopyrightText: � 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: � 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.Client.Common
{
    [Serializable]
    public class ProblemException : Exception
    {
        public ProblemDetails ProblemDetails { get; private set; }

        public ProblemException(ProblemDetails problemDetails) : base(problemDetails?.Detail)
        {
            ProblemDetails = problemDetails ?? throw new ArgumentNullException(nameof(problemDetails));
        }

        protected ProblemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string Message => ToString();

        public override string ToString()
        {
            return $"HTTP Status: {ProblemDetails.Status}. {ProblemDetails.Detail}";
        }
    }
}
