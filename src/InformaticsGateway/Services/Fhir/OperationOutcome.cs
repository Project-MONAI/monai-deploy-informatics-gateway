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
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Monai.Deploy.InformaticsGateway.Services.Fhir
{
    [DataContract(Namespace = Resources.FhirXmlNamespace)]
    public class OperationOutcome
    {
        [DataMember]
        public string ResourceType { get; set; }

        [DataMember]
        public string Id { get; set; }

        [XmlIgnore, JsonPropertyName("issue")]
        public IList<Issue> Issues { get; set; }

        [DataMember, JsonIgnore]
        public Issue Issue
        {
            get
            {
                return Issues.FirstOrDefault();
            }
            set
            {
                Issues.Clear();
                Issues.Add(value);
            }
        }

        public OperationOutcome()
        {
            Issues = new List<Issue>();
        }
    }

    [DataContract(Namespace = Resources.FhirXmlNamespace)]
    public class Issue
    {
        [DataMember]
        public IssueSeverity Severity { get; set; }

        [DataMember]
        public IssueType Code { get; set; }

        [DataMember]
        public IList<IssueDetails> Details { get; set; }

        public Issue()
        {
            Details = new List<IssueDetails>();
        }
    }

    [DataContract(Namespace = Resources.FhirXmlNamespace)]
    public class IssueDetails
    {
        [DataMember]
        public string Text { get; set; }
    }

    [DataContract(Namespace = Resources.FhirXmlNamespace)]
    public enum IssueType
    {
        [EnumMember]
        Invalid,

        [EnumMember]
        Structure,

        [EnumMember]
        Exception
    }

    [DataContract(Namespace = Resources.FhirXmlNamespace)]
    public enum IssueSeverity
    {
        [EnumMember]
        Information,

        [EnumMember]
        Warning,

        [EnumMember]
        Error,

        [EnumMember]
        Fatal,
    }
}
