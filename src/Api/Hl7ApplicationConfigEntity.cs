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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Api
{
    public class Hl7ApplicationConfigEntity : MongoDBEntityBase
    {
        /// <summary>
        /// Gets or sets the name of a Hl7 application entity.
        /// This value must be unique.
        /// </summary>
        [Key, Column(Order = 0)]
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets the sending identifier.
        /// </summary>
        [JsonProperty("sending_identifier")]
        public StringKeyValuePair SendingId { get; set; } = new();

        /// <summary>
        /// Gets or sets the data link.
        /// Value is either PatientId or StudyInstanceUid
        /// </summary>
        [JsonProperty("data_link")]
        public DataKeyValuePair DataLink { get; set; } = new();

        /// <summary>
        /// Gets or sets the data mapping.
        /// Value is a DICOM Tag
        /// </summary>
        [JsonProperty("data_mapping")]
        public List<StringKeyValuePair> DataMapping { get; set; } = new();

        /// <summary>
        /// Optional list of data input plug-in type names to be executed by the <see cref="IInputHL7DataPlugInEngine"/>.
        /// </summary>
        public List<string> PlugInAssemblies { get; set; } = default!;

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(SendingId.Key))
                errors.Add($"{nameof(SendingId.Key)} is missing.");
            if (string.IsNullOrWhiteSpace(SendingId.Value))
                errors.Add($"{nameof(SendingId.Value)} is missing.");

            if (string.IsNullOrWhiteSpace(DataLink.Key))
                errors.Add($"{nameof(DataLink.Key)} is missing.");

            if (DataMapping.IsNullOrEmpty())
                errors.Add($"{nameof(DataMapping)} is missing values.");

            ValidateDataMapping(errors);

            return errors;
        }

        private void ValidateDataMapping(List<string> errors)
        {
            for (var idx = 0; idx < DataMapping.Count; idx++)
            {
                var dataMapKvp = DataMapping[idx];

                if (string.IsNullOrWhiteSpace(dataMapKvp.Key) || dataMapKvp.Value.Length < 8)
                {
                    if (string.IsNullOrWhiteSpace(dataMapKvp.Key))
                        errors.Add($"{nameof(DataMapping)} is missing a name at index {idx}.");

                    if (string.IsNullOrWhiteSpace(dataMapKvp.Value) || dataMapKvp.Value.Length < 8)
                        errors.Add($"{nameof(DataMapping)} ({dataMapKvp.Key}) @ index {idx} is not a valid DICOM Tag.");

                    continue;
                }

                try
                {
                    DicomTag.Parse(dataMapKvp.Value);
                }
                catch (Exception e)
                {
                    errors.Add($"DataMapping.Value is not a valid DICOM Tag. {e.Message}");
                }
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    //string key, string value
    public class StringKeyValuePair : IKeyValuePair<string, string>
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public static implicit operator StringKeyValuePair(KeyValuePair<string, string> kvp)
        {
            return new StringKeyValuePair { Key = kvp.Key, Value = kvp.Value };
        }

        public static List<StringKeyValuePair> FromDictionary(Dictionary<string, string> dictionary) =>
            dictionary.Select(kvp => new StringKeyValuePair { Key = kvp.Key, Value = kvp.Value }).ToList();

        public override bool Equals(object? obj) => Equals(obj as StringKeyValuePair);

        public bool Equals(StringKeyValuePair? other) => other != null && Key == other.Key && Value == other.Value;

        public override int GetHashCode() => HashCode.Combine(Key, Value);

    }

    public class DataKeyValuePair : IKeyValuePair<string, DataLinkType>
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public DataLinkType Value { get; set; }

        public static implicit operator DataKeyValuePair(KeyValuePair<string, DataLinkType> kvp)
        {
            return new DataKeyValuePair { Key = kvp.Key, Value = kvp.Value };
        }

        public override bool Equals(object? obj) => Equals(obj as DataKeyValuePair);

        public bool Equals(DataKeyValuePair? other) => other != null && Key == other.Key && Value == other.Value;

        public override int GetHashCode() => HashCode.Combine(Key, Value);
    }

    public interface IKeyValuePair<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }

    public enum DataLinkType
    {
        PatientId,
        StudyInstanceUid
    }
}
