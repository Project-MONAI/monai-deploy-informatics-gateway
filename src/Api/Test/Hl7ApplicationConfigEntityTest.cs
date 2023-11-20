/*
 * Copyright 2021-2023 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using System.Globalization;
using Newtonsoft.Json;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class Hl7ApplicationConfigEntityTest
    {
        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenSendingIdKeyIsNotSet_ExpectValidateToReturnError()
        {
            var entity = new Hl7ApplicationConfigEntity
            {
                SendingId = new KeyValuePair<string, string>(string.Empty, "SendingIdValue"),
                DataLink = new KeyValuePair<string, DataLinkType>("DataLinkKey", DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string> { { "DataMappingKey", "DataMappingValue" } }
            };

            var errors = entity.Validate();

            Assert.NotEmpty(errors);
            Assert.Contains($"{nameof(entity.SendingId.Key)} is missing.", errors);
        }

        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenSendingIdValueIsNotSet_ExpectValidateToReturnError()
        {
            var entity = new Hl7ApplicationConfigEntity
            {
                SendingId = new KeyValuePair<string, string>("SendingIdKey", string.Empty),
                DataLink = new KeyValuePair<string, DataLinkType>("DataLinkKey", DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string> { { "DataMappingKey", "DataMappingValue" } }
            };

            var errors = entity.Validate();

            Assert.NotEmpty(errors);
            Assert.Contains($"{nameof(entity.SendingId.Value)} is missing.", errors);
        }

        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenDataLinkKeyIsNotSet_ExpectValidateToReturnError()
        {
            var entity = new Hl7ApplicationConfigEntity
            {
                SendingId = new KeyValuePair<string, string>("SendingIdKey", "SendingIdValue"),
                DataLink = new KeyValuePair<string, DataLinkType>(string.Empty, DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string> { { "DataMappingKey", "DataMappingValue" } }
            };

            var errors = entity.Validate();

            Assert.NotEmpty(errors);
            Assert.Contains($"{nameof(entity.DataLink.Key)} is missing.", errors);
        }

        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenDataMappingIsNotSet_ExpectValidateToReturnError()
        {
            var entity = new Hl7ApplicationConfigEntity
            {
                SendingId = new KeyValuePair<string, string>("SendingIdKey", "SendingIdValue"),
                DataLink = new KeyValuePair<string, DataLinkType>("DataLinkKey", DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string>()
            };

            var errors = entity.Validate();

            Assert.NotEmpty(errors);
            Assert.Contains($"{nameof(entity.DataMapping)} is missing values.", errors);
        }

        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenDataMappingKeyIsNotSet_ExpectValidateToReturnError()
        {
            var entity = new Hl7ApplicationConfigEntity
            {
                SendingId = new KeyValuePair<string, string>("SendingIdKey", "SendingIdValue"),
                DataLink = new KeyValuePair<string, DataLinkType>("DataLinkKey", DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string> { { string.Empty, "DataMappingValue" } }
            };

            var errors = entity.Validate();

            Assert.NotEmpty(errors);
            Assert.Contains($"{nameof(entity.DataMapping)} is missing a name at index 0.", errors);
        }

        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenDataMappingValueIsNotSet_ExpectValidateToReturnError()
        {
            var entity = new Hl7ApplicationConfigEntity
            {
                SendingId = new KeyValuePair<string, string>("SendingIdKey", "SendingIdValue"),
                DataLink = new KeyValuePair<string, DataLinkType>("DataLinkKey", DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string> { { "DataMappingKey", string.Empty } }
            };

            var errors = entity.Validate();

            Assert.NotEmpty(errors);
            Assert.Contains($"{nameof(entity.DataMapping)} (DataMappingKey) @ index 0 is not a valid DICOM Tag.", errors);
        }

        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenDataMappingValueIsNotAValidDicomTag_ExpectValidateToReturnError()
        {
            var entity = new Hl7ApplicationConfigEntity
            {
                SendingId = new KeyValuePair<string, string>("SendingIdKey", "SendingIdValue"),
                DataLink = new KeyValuePair<string, DataLinkType>("DataLinkKey", DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string> { { "DataMappingKey", "DataMappingValue" } }
            };

            var errors = entity.Validate();

            Assert.NotEmpty(errors);
            Assert.Contains("DataMapping.Value is not a valid DICOM Tag. Error parsing DICOM tag ['DataMappingValue']", errors);
        }

        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenDataMappingValueIsAValidDicomTag_ExpectValidateToReturnNoErrors()
        {
            var entity = new Hl7ApplicationConfigEntity
            {
                SendingId = new KeyValuePair<string, string>("SendingIdKey", "SendingIdValue"),
                DataLink = new KeyValuePair<string, DataLinkType>("DataLinkKey", DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string> { { "DataMappingKey", "0020,000D" } }
            };

            var errors = entity.Validate();

            Assert.Empty(errors);
        }

        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenDataMappingValueIsEmpty_ExpectValidateToReturnError()
        {
            var entity = new Hl7ApplicationConfigEntity
            {
                SendingId = new KeyValuePair<string, string>("SendingIdKey", "SendingIdValue"),
                DataLink = new KeyValuePair<string, DataLinkType>("DataLinkKey", DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string> { { "DataMappingKey", "" } }
            };

            var errors = entity.Validate();

            Assert.NotEmpty(errors);
            Assert.Contains($"{nameof(entity.DataMapping)} (DataMappingKey) @ index 0 is not a valid DICOM Tag.", errors);
        }

        [Fact]
        public void GivenAHl7ApplicationConfigEntity_WhenToStringIsCalled_ExpectToStringToReturnExpectedValue()
        {
            var guid = Guid.NewGuid();
            var dt = DateTime.UtcNow;
            var entity = new Hl7ApplicationConfigEntity
            {
                Id = guid,
                DateTimeCreated = dt,
                SendingId = new KeyValuePair<string, string>("SendingIdKey", "SendingIdValue"),
                DataLink = new KeyValuePair<string, DataLinkType>("DataLinkKey", DataLinkType.PatientId),
                DataMapping = new Dictionary<string, string> { { "DataMappingKey", "0020,000D" } }
            };

            var result = entity.ToString();

            var expected = JsonConvert.SerializeObject(entity);
            Assert.Equal(expected, result);
        }
    }
}
