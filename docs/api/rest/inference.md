<!--
  ~ Copyright 2021-2022 MONAI Consortium
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

# Inference Request APIs

The inference endpoint provides a RESTful interface for triggering a new inference request that is compatible with the ACR DSI API.

> [!Warning]
> This API is a work in progress and may change between releases.

> [!Note]
> The inference API is extended based on the draft created by the ACR (American College of Radiology).
> Please refer to [ACR's Platform-Model Communication for AI](https://www.acrdsi.org/-/media/DSI/Files/ACR-DSI-Model-API.pdf)
> for more information.

## POST /inference

Triggers a new inference job using the specified DICOM dataset from the specified data sources.


> [!IMPORTANT]
> For input and output connections that require credentials, ensure that all the connections are secured and encrypted.

### Parameters

See the [InferenceRequest](xref:Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequest) class
definition for examples.

Request Content Type: JSON

| Name            | Type                                                                                                      | Description                                                                                                                                                                                          |
| --------------- | --------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| transactionID   | string                                                                                                    | (**Required**) A user-provided transaction ID for correlating an inference request.                                                                                                                  |
| priority        | number                                                                                                    | The valid range is 0-255. Please refer to [Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequest.Priority](xref:Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequest.Priority) for details. |
| inputMetadata   | [inputMetadata](xref:Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequestMetadata) object            | (**Required**) The dataset associated with the inference request.                                                                                                                                    |
| inputResources  | array of [inputResource](xref:Monai.Deploy.InformaticsGateway.Api.Rest.RequestInputDataResource) objects  | (**Required**) Data sources from which to retrieve the specified dataset. **MONAI Deploy Only**: Must include one `interface` that is type of `Algorithm`.                                           |
| outputResources | array of [inputResource](xref:Monai.Deploy.InformaticsGateway.Api.Rest.RequestOutputDataResource) objects | (**Required**) Output destinations where results are exported to                                                                                                                                     |

### Responses

Response Content Type: JSON - [InferenceRequestResponse](xref:Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequestResponse).

| Code | Description                                                                                                                                                                                 |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | Inference request received and scheduled for processing.                                                                                                                                    |
| 409  | A request with the same transaction ID already exists. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.            |
| 422  | Request contains invalid data or is missing required fields. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.      |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.                                                     |

---

## GET /inference/status/{transactionId}

Returns the status of an inference request.

### Parameters

| Name          | Type   | Description                                            |
| ------------- | ------ | ------------------------------------------------------ |
| transactionId | string | The _transactionId_ of the original inference request  |

### Responses

Response Content Type: JSON - [InferenceStatusResponse](xref:Monai.Deploy.InformaticsGateway.Api.Rest.InferenceStatusResponse).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | Inference request status is available.                                                                                                  |
| 404  | Inference request not found.                                                                                                            |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |
