﻿/*
 * Copyright 2022 MONAI Consortium
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

namespace Monai.Deploy.InformaticsGateway.Services.Fhir
{
    [Serializable]
    public class FhirStoreException : Exception
    {
        public OperationOutcome OperationOutcome { get; }

        public FhirStoreException(string correlationId, string message, IssueType issueType) : this(correlationId, message, issueType, null)
        {
        }

        public FhirStoreException(string correlationId, string message, IssueType issueType, Exception innerException) : base(message, innerException)
        {
            OperationOutcome = new OperationOutcome()
            {
                Id = correlationId,
                ResourceType = Resources.ResourceOperationOutcome,
            };

            OperationOutcome.Issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Code = issueType
            });

            OperationOutcome.Issues[0].Details.Add(new IssueDetails
            {
                Text = message
            });
        }

        protected FhirStoreException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
