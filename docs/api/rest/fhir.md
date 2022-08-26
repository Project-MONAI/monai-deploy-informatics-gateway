<!--
  ~ Copyright 2022 MONAI Consortium
  ~
  ~ Licensed under the Apache License, Version 2.0 (the "License");
  ~ you may not use this file except in compliance with the License.
  ~ You may obtain a copy of the License at
  ~
  ~ http://www.apache.org/licenses/LICENSE-2.0
  ~
  ~ Unless required by applicable law or agreed to in writing, software
  ~ distributed under the License is distributed on an "AS IS" BASIS,
  ~ WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  ~ See the License for the specific language governing permissions and
  ~ limitations under the License.
-->

# DICOMWeb STOW-RS APIs

The `fhir/` endpoint implements the specifications defined in [section 3.1.0 RESTful API](http://hl7.org/implement/standards/fhir/http.html)
defined by HL7 (Health Level 7 International) to enable triggering new workflows. The FHIR service supports multiple versions of the Fast Healthcare Interoperability Resources (FHIR) specifications published by Health Level 7 International (HL7).

[!Note]
The service does not support `CapabilityStatement` at this moment and does not group the incoming FHIR resources. Therefore, each incoming FHIR resource will trigger a new workflow request.

The *FHIR* service provides the following endpoint.

## POST /fhir/[resource]

Triggers a new workflow request with the posted FHIR resource.

> [!IMPORTANT]
> Refer to [section 1.2 Resource Index](http://hl7.org/fhir/resourcelist.html) for a list of HL7 resources. The endpoint is designed to accept any resource and provides only syntax validation either in XML or JSON.

### Parameters

#### Query Parameters:

| Name     | Type   | Description                                                                                                                                                                                |
| -------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| resource | string | (Optional) resouce type of the FHIR document. The services reject the request if the `Resource` value in the URL differs from the `Resource` value described in the posted document. |

#### Request Body: 

Supported Content-Types:

- `application/fhir+json`
- `application/fhir+xml`

### Responses

Depending on the `Accept` header or the original document, the response supports the following content types: 

- `application/fhir+json`
- `application/fhir+xml`

If the `Accept` header is missing or a none supported value exists, the service will return the same type as the posted document.

| Code | Data Type                                                     | Description                                                           |
| ---- | ------------------------------------------------------------- | --------------------------------------------------------------------- |
| 201  | Original JSON or XML document.                                | Resource created & stored successfully.                               |
| 400  | [OperationOutcome](http://hl7.org/fhir/operationoutcome.html) | Unable to parse the resource or mismatching resource type specified.. |
| 415  | `none`                                                        | Unsupported media type                                                |
| 500  | [OperationOutcome](http://hl7.org/fhir/operationoutcome.html) | Server error.                                                         |

[!Note]
The `Location` header in the response given that the resources created are for inference only.
