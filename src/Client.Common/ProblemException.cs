/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2021 NVIDIA Corporation
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
            Guard.Against.Null(problemDetails);

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
                throw new ArgumentNullException(nameof(info));
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
