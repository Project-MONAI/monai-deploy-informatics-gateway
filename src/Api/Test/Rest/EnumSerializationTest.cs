// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Text.Json;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test.Rest
{
    public class EnumSerializationTest
    {
        [Theory(DisplayName = "InputInterfaceType serializes using enum member value")]
        [InlineData(InputInterfaceType.Algorithm, "Algorithm")]
        [InlineData(InputInterfaceType.DicomWeb, "DICOMweb")]
        [InlineData(InputInterfaceType.Dimse, "DIMSE")]
        [InlineData(InputInterfaceType.Fhir, "FHIR")]
        public void InputInterfaceTypeTest(InputInterfaceType @type, string expectedStringValue)
        {
            var json = JsonSerializer.Serialize(@type);
            Assert.Equal($"\"{expectedStringValue}\"", json);
            var @enum = JsonSerializer.Deserialize<InputInterfaceType>(json);
            Assert.Equal(@type, @enum);
        }

        [Theory(DisplayName = "InputInterfaceType serializes using enum member value")]
        [InlineData(InferenceRequestType.DicomUid, "DICOM_UID")]
        [InlineData(InferenceRequestType.DicomPatientId, "DICOM_PATIENT_ID")]
        [InlineData(InferenceRequestType.AccessionNumber, "ACCESSION_NUMBER")]
        [InlineData(InferenceRequestType.FhireResource, "FHIR_RESOURCE")]
        public void InferenceRequestTypeTest(InferenceRequestType @type, string expectedStringValue)
        {
            var json = JsonSerializer.Serialize(@type);
            Assert.Equal($"\"{expectedStringValue}\"", json);
            var @enum = JsonSerializer.Deserialize<InferenceRequestType>(json);
            Assert.Equal(@type, @enum);
        }

        [Theory(DisplayName = "InputInterfaceType serializes using enum member value")]
        [InlineData(InputInterfaceOperations.Query, "QUERY")]
        [InlineData(InputInterfaceOperations.Retrieve, "RETRIEVE")]
        [InlineData(InputInterfaceOperations.WadoRetrieve, "WADO Retrieve")]
        [InlineData(InputInterfaceOperations.Store, "STORE")]
        public void InputInterfaceOperationsTest(InputInterfaceOperations @type, string expectedStringValue)
        {
            var json = JsonSerializer.Serialize(@type);
            Assert.Equal($"\"{expectedStringValue}\"", json);
            var @enum = JsonSerializer.Deserialize<InputInterfaceOperations>(json);
            Assert.Equal(@type, @enum);
        }
    }
}
