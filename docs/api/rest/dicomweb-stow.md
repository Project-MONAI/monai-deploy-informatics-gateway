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

The `dicomweb/` endpoint implements the specifications defined in [section 6.6 STOW-RS Request/Response](https://dicom.nema.org/dicom/2013/output/chtml/part18/sect_6.6.html#sect_6.6.1.3.2.1.1)
defined by the DICOM committee to provide the [DICOMWeb STOW-RS](https://www.dicomstandard.org/using/dicomweb/store-stow-rs)
interface for triggering new workflows.

The *STOW-RS* service provides the following two endpoints.

## POST /dicomweb/studies/[{study-instance-uid}/]

Triggers a new workflow request with the uploaded DICOM dataset.

> [!IMPORTANT]
> Each HTTP POST request triggers a new workflow request; the service *does not* support waiting
  for additional instances like the DIMSE service.

### Example Endpoints

- `POST /dicomweb/studies/`
- `POST /dicomweb/studies/123.001.123.1.4.976.20160825112022727.3/`

### Parameters

#### Query Parameters

| Name               | Type   | Description                                                                                                                                                                                                                     |
| ------------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| study-instance-uid | string | (Optional) Associate the DICOM dataset with a StudyInstanceUID. Note that the service records any mismatch between the StudyInstanceUID header and the provided value in the response as `Warning Reason (0008,1196)` = `B007`. |

#### Request Body

Supported Content-Types:

- `application/dicom`
- `multipart/related`

### Responses

Response Content Type: `JSON`

| Code | Data Type                                                                                           | Description                                                                                  |
| ---- | --------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| 200  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | All instances are received and stored successfully.                                          |
| 202  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | All instances are received and stored with warnings (e.g. for a mismatched StudyInstanceUID. |
| 204  | `none`                                                                                              | No data is provided.                                                                         |
| 400  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Request contains invalid values.                                                             |
| 415  | `none`                                                                                              | Unsupported media type.                                                                      |
| 500  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Server error.                                                                                |
| 507  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Insufficient storage.                                                                        |

---

## POST /dicomweb/{workflow-id}/studies/[{study-instance-uid}/]

Triggers the specified workflow with the uploaded DICOM dataset.

> [!IMPORTANT]
> Each HTTP POST request triggers a new workflow request; the service *does not* support waiting for additional instances like the DIMSE service.

### Example Endpoints

- `POST /dicomweb/liver-segmentation/studies/`
- `POST /dicomweb/my-awesome-workflow/studies/123.001.123.1.4.976.20160825112022727.3/`

### Parameters

#### Query Parameters

| Name               | Type   | Description                                                                                                                                                                                                                     |
| ------------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| workflow-id        | string | The unique identifier of the workflow registered with the Workflow Manager.                                                                                                                                                     |
| study-instance-uid | string | (Optional) Associate the DICOM dataset with a StudyInstanceUID. Note that the service records any mismatch between the StudyInstanceUID header and the provided value in the response as `Warning Reason (0008,1196)` = `B007`. |

#### Request Body

Supported Content-Types:

- `application/dicom`
- `multipart/related`

### Responses

Response Content Type: `JSON`

| Code | Data Type                                                                                           | Description                                                                                  |
| ---- | --------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| 200  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | All instances are received and stored successfully.                                          |
| 202  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | All instances are received and stored with warnings (e.g. for a mismatched StudyInstanceUID. |
| 204  | `none`                                                                                              | No data is provided.                                                                         |
| 400  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Request contains invalid values.                                                             |
| 415  | `none`                                                                                              | Unsupported media type.                                                                      |
| 500  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Server error.                                                                                |
| 507  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Insufficient storage.                                                                        |

---

## POST /dicomweb/vae/{aet}/[{workflow-id}/]studies/[{study-instance-uid}/]

A DICOMWeb STOW-RS endpoint associated with the specified [Virtual Application Entity](xref:Monai.Deploy.InformaticsGateway.Api.VirtualApplicationEntity).

This endpoint can either trigger workflows defined in a [Virtual Application Entity](xref:Monai.Deploy.InformaticsGateway.Api.VirtualApplicationEntity) or trigger the workflow specified in the URL segment where the latter
takes precedence when specified.

> [!IMPORTANT]
> Each HTTP POST request triggers a new workflow request; the service *does not* support waiting for additional instances like the DIMSE service.

### Example Endpoints

- `POST /dicomweb/vae/my-aet/studies/`
- `POST /dicomweb/vae/my-aet/studies/123.001.123.1.4.976.20160825112022727.3/`
- `POST /dicomweb/vae/my-aet/my-awesome-workflow/studies/`
- `POST /dicomweb/vae/my-aet/my-awesome-workflow/studies/123.001.123.1.4.976.20160825112022727.3/`

### Parameters

#### Query Parameters

| Name               | Type   | Description                                                                                                                                                                                                                     |
| ------------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| aet                | string | A registered Virtual Application Entity.                                                                                                                                                                                        |
| workflow-id        | string | The unique identifier of the workflow registered with the Workflow Manager.                                                                                                                                                     |
| study-instance-uid | string | (Optional) Associate the DICOM dataset with a StudyInstanceUID. Note that the service records any mismatch between the StudyInstanceUID header and the provided value in the response as `Warning Reason (0008,1196)` = `B007`. |

#### Request Body

Supported Content-Types:

- `application/dicom`
- `multipart/related`

### Responses

Response Content Type: `JSON`

| Code | Data Type                                                                                           | Description                                                                                  |
| ---- | --------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| 200  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | All instances are received and stored successfully.                                          |
| 202  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | All instances are received and stored with warnings (e.g. for a mismatched StudyInstanceUID. |
| 204  | `none`                                                                                              | No data is provided.                                                                         |
| 400  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Request contains invalid values.                                                             |
| 415  | `none`                                                                                              | Unsupported media type.                                                                      |
| 500  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Server error.                                                                                |
| 507  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Insufficient storage.                                                                        |
