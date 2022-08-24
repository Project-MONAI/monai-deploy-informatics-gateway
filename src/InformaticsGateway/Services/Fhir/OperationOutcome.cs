/*
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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Monai.Deploy.InformaticsGateway.Services.Fhir
{
    public class OperationOutcome
    {
        public string ResourceType { get; set; }
        public string Id { get; set; }

        [JsonPropertyName("issue")]
        public IList<Issue> Issues { get; set; }

        public OperationOutcome()
        {
            Issues = new List<Issue>();
        }
    }

    public class Issue
    {
        public IssueSeverity Severity { get; set; }
        public IssueType Code { get; set; }
        public IList<IssueDetails> Details { get; set; }

        public Issue()
        {
            Details = new List<IssueDetails>();
        }
    }

    public class IssueDetails
    {
        public string Text { get; set; }
    }

    public enum IssueType
    {
        Invalid,
        Structure,
        Exception
    }

    public enum IssueSeverity
    {
        Information,
        Warning,
        Error,
        Fatal,
    }
}
