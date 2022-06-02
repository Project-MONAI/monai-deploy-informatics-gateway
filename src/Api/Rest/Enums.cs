// SPDX-FileCopyrightText: © 2011-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Specifies then authentication/authorization type for a connection.
    /// </summary>
    public enum ConnectionAuthType
    {
        /// <summary>
        /// No authentication required.
        /// (Default) if not specified.
        /// <para><c>JSON value</c>: <c>None</c></para>
        /// </summary>
        None,

        /// <summary>
        /// HTTP Basic access authentication.
        /// <para><c>JSON value</c>: <c>Basic</c></para>
        /// </summary>
        Basic,

        /// <summary>
        /// OAuth 2.0 Bearer authentication/authorization.
        /// <para><c>JSON value</c>: <c>Bearer</c></para>
        /// </summary>
        Bearer,
    }

    /// <summary>
    /// Specifies the type of data source interface.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum InputInterfaceType
    {
        /// <summary>
        /// MONAI Deploy only - specifies a MONAI Application to trigger with the request
        /// <para><c>JSON value</c>: <c>Algorithm</c></para>
        /// </summary>
        [EnumMember(Value = "Algorithm")]
        Algorithm,

        /// <summary>
        /// Retrieves data using DICOMweb API
        /// <para><c>JSON value</c>: <c>DICOMweb</c></para>
        /// </summary>
        [EnumMember(Value = "DICOMweb")]
        DicomWeb,

        /// <summary>
        /// Retrieves data using TCP based DICOM DIMSE services
        /// <para><c>JSON value</c>: <c>DIMSE</c></para>
        /// </summary>
        [EnumMember(Value = "DIMSE")]
        Dimse,

        /// <summary>
        /// Retrieves data via FHIR.
        /// <para><c>JSON value</c>: <c>FHIR</c></para>
        /// </summary>
        [EnumMember(Value = "FHIR")]
        Fhir,
    }

    /// <summary>
    /// Specifies type of inference request.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum InferenceRequestType
    {
        /// <summary>
        /// Unknown request type
        /// </summary>
        Unknown,

        /// <summary>
        /// Retrieves dataset specified using DICOM UIDs
        /// <para><c>JSON value</c>: <c>DICOM_UID</c></para>
        /// </summary>
        [EnumMember(Value = "DICOM_UID")]
        DicomUid,

        /// <summary>
        /// Queries the data source using Patient ID and retrieves any associated studies.
        /// <para><c>JSON value</c>: <c>DICOM_PATIENT_ID</c></para>
        /// </summary>
        [EnumMember(Value = "DICOM_PATIENT_ID")]
        DicomPatientId,

        /// <summary>
        /// Queries the data source using Accession Number and retrieves any associated studies.
        /// <para><c>JSON value</c>: <c>ACCESSION_NUMBER</c></para>
        /// </summary>
        [EnumMember(Value = "ACCESSION_NUMBER")]
        AccessionNumber,

        /// <summary>
        /// Retrieves data from a FHIR server using specified resource type and ID.
        /// <para><c>JSON value</c>: <c>FHIR_RESOURCE</c></para>
        /// </summary>
        [EnumMember(Value = "FHIR_RESOURCE")]
        FhireResource,
    }

    /// <summary>
    /// Permitted operations for a data source
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum InputInterfaceOperations
    {
        /// <summary>
        /// Query includes C-FIND, QIDO operations
        /// <para><c>JSON value</c>: <c>QUERY</c></para>
        /// </summary>
        [EnumMember(Value = "QUERY")]
        Query,

        /// <summary>
        /// Retrieve include C-MOVE, WADO operations
        /// <para><c>JSON value</c>: <c>RETRIEVE</c></para>
        /// </summary>
        [EnumMember(Value = "RETRIEVE")]
        Retrieve,

        /// <summary>
        /// DICOMweb WADO
        /// <para><c>JSON value</c>: <c>WADO Retrieve</c></para>
        /// </summary>
        [EnumMember(Value = "WADO Retrieve")]
        WadoRetrieve,

        /// <summary>
        /// Store includes C-STORE, STOW operations
        /// <para><c>JSON value</c>: <c>STORE</c></para>
        /// </summary>
        [EnumMember(Value = "STORE")]
        Store,
    }
}
