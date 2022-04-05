// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.Client.Common
{
    [Serializable]
    public class ProblemException : Exception
    {
        public ProblemDetails ProblemDetails { get; private set; }

        public ProblemException(ProblemDetails problemDetails) : base(problemDetails?.Detail)
        {
            Guard.Against.Null(problemDetails, nameof(problemDetails));

            ProblemDetails = problemDetails;
        }

        protected ProblemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ProblemDetails = (ProblemDetails)info.GetValue(nameof(ProblemDetails), typeof(ProblemDetails));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue(nameof(ProblemDetails), ProblemDetails, typeof(ProblemDetails));

            base.GetObjectData(info, context);
        }

        public override string Message => ToString();

        public override string ToString()
        {
            return $"HTTP Status: {ProblemDetails.Status}. {ProblemDetails.Detail}";
        }
    }
}
