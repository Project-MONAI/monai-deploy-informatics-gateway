<!--
SPDX-FileCopyrightText: © 2022 MONAI Consortium
SPDX-License-Identifier: Apache License 2.0
-->

# DICOMWeb STOW-RS APIs

The `dicomweb/` endpoint implements the specifications defined in section [6.6 STOW-RS Request/Response](https://dicom.nema.org/dicom/2013/output/chtml/part18/sect_6.6.html#sect_6.6.1.3.2.1.1) defined by the DICOM committee to provide the [DICOMWeb STOW-RS](https://www.dicomstandard.org/using/dicomweb/store-stow-rs) interface to enable the triggering of new workflows. 


The *STOW-RS* service provides the following two endpoints:

## POST /dicomweb/studies/[{study-instance-uid}]

Triggers a new workflow request with the uploaded DICOM dataset.

> [!IMPORTANT]
> Each HTTP POST request triggers a new workflow request; the service *does not* support waiting for additional instances like the DIMSE service.

### Parameters

#### Query Parameters:

| Name               | Type   | Description                                                                                                                                                                                                                                 |
| ------------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| study-instance-uid | string | Optionally associate the DICOM dataset with a StudyInstanceUID. Note: the service records any mismatch in the  StudyInstanceUID header to the provided value in the response with `Warning Reason (0008,1196)` = `B007`. |

#### Request Body: 

Supported Content-Types:

- `application/dicom`
- `multipart/related`

### Responses

Response Content Type: `JSON`

| Code | Data Type                                                                                           | Description                                                                          |
| ---- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| 200  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | All instances are received and stored succesfully.                                   |
| 202  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | ALl instances are received and stored with warnings. E.g. mismatch StudyInstanceUID. |
| 204  | `none`                                                                                              | No data provided.                                                                    |
| 400  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Request contains invalid values.                                                     |
| 415  | `none`                                                                                              | Unsupported media type.                                                              |
| 500  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Server error.                                                                        |

---


## POST /dicomweb/{workflow-id}/studies/[{study-instance-uid}]

Triggers the specified workflow with the uploaded DICOM dataset.

> [!IMPORTANT]
> Each HTTP POST request triggers a new workflow request; the service *does not* support waiting for additional instances like the DIMSE service.

### Parameters

#### Query Parameters:

| Name               | Type   | Description                                                                                                                                                                                                                                 |
| ------------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| workflow-id | string | The unique identifier of the workflow registered with the Workflow Manager. |
| study-instance-uid | string | Optionally associate the DICOM dataset with a StudyInstanceUID. Note: the service records any mismatch in the  StudyInstanceUID header to the provided value in the response with `Warning Reason (0008,1196)` = `B007`. |

#### Request Body: 

Supported Content-Types:

- `application/dicom`
- `multipart/related`

### Responses

Response Content Type: `JSON`

| Code | Data Type                                                                                           | Description                                                                          |
| ---- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| 200  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | All instances are received and stored succesfully.                                   |
| 202  | [DicomDataset](https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs) | ALl instances are received and stored with warnings. E.g. mismatch StudyInstanceUID. |
| 204  | `none`                                                                                              | No data provided.                                                                    |
| 400  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Request contains invalid values.                                                     |
| 415  | `none`                                                                                              | Unsupported media type.                                                              |
| 500  | [Problem details](https://datatracker.ietf.org/doc/html/rfc7807)                                    | Server error.                                                                        |
