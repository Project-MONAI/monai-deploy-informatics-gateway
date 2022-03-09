// SPDX-FileCopyrightText: © 2011-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Data format used for storing retrived FHIR resources.
    /// Reference: <see href="https://www.hl7.org/fhir/http.html#mime-type">FHIR: Content Types and encodings</see>.
    /// </summary>
    public enum FhirStorageFormat
    {
        /// <summary>
        /// application/fhir+json
        /// </summary>
        Json,

        /// <summary>
        /// application/fhir+xml
        /// </summary>
        Xml
    }

    /// <summary>
    /// Version of FHIR release a resource is based on.
    /// Reference: <see href="https://www.hl7.org/fhir/http.html#version-parameter">FHIR:  Version Parameter</see>.
    /// </summary>
    public enum FhirVersion
    {
        /// <summary>
        /// DSTU 1
        /// </summary>
        R1,

        /// <summary>
        /// DSTU 2
        /// </summary>
        R2,

        /// <summary>
        /// STU3 or R3
        /// </summary>
        R3,

        /// <summary>
        /// R4
        /// </summary>
        R4
    }

    /// <summary>
    /// Represents a FHIR resource to be retrieved.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     "resourceType": "Patient",
    ///     "id": "3d2e37f0-c6bf-4479-bba8-3ca86cbb8a58"
    /// }
    /// </code>
    /// </example>
    public class FhirResource
    {
        /// <summary>
        /// Gets or set the type of FHIR resource.
        /// E.g. Pateitn, Observation.
        /// </summary>
        [JsonProperty(PropertyName = "resourceType")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the ID of the resource to be retrieved.
        /// </summary>
        /// <value></value>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Internal use only!
        /// Gets or sets whether or not resource has been retrieved or not.
        /// </summary>
        /// <value></value>
        [JsonProperty(PropertyName = "isRetrieved")]
        public bool IsRetrieved { get; set; }
    }
}
