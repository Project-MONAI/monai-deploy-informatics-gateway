# MONAI Deploy Informatics Gateway Software Architecture & Design

## Overview

The MONAI Deploy Informatics Gateway (MIG) is the integration point between hospital information systems (HIS) and the MONAI Deploy platform. It enables interoperability between HIS and the MONAI Deploy platform by using commonly used standards in the healthcare industry.

### Purpose

This document describes the detail designs derived from the requirements defined in [MONAI Deploy Informatics Gateway Requirements](informatics-gateway-requirements.md).

### Scope

The scope of this document is limited to the design of MONAI Deploy Informatics Gateway. This design document does not address any design decisions belonging to other subsystems, such as, MONAI App Server, MONAI Deploy Application SDK.

### Assumptions, Constraints, Dependencies

1. No data validation is done on the received or retrieved dataset, including but not limited to, DICOM and FHIR. The data processing consumer/user shall validate incoming data as part of the workflow.
1. MONAI Deploy Informatics Gateway is not intended for long term DICOM storage and does not support Storage Commitment Requests. See implementation details for each of the bundled job processors.

### Definitions, Acronyms, Abbreviations

| Term            | Definition                                                                                                                                                                                      |
| --------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| MIG             | MONAI Deploy Informatics Gateway                                                                                                                                                                       |
| MWM             | MONAI Workload Manager - A subsystem of the MONAI Deploy platform responsible for routing incoming data to one or more deployed applications and export any results produced by the applications to external HIS devices. |
| AE              | [Application Entity](http://dicom.nema.org/medical/dicom/current/output/chtml/part02/sect_A.3.4.html)                                                                                           |
| AE Title        | [Application Entity Title](http://dicom.nema.org/medical/dicom/current/output/chtml/part02/sect_A.3.4.html) (AET)                                                                               |
| DICOM           | [Digital Imaging and Communications in Medicine](https://www.dicomstandard.org/)                                                                                                                |
| DICOM Tag       | or simply ["Tag"](http://dicom.nema.org/medical/dicom/current/output/chtml/part02/sect_A.3.4.html)                                                                                              |
| FHIR            | [Fast Healthcare Interoperability Resources](https://en.wikipedia.org/wiki/Fast_Healthcare_Interoperability_Resources)                                                                          |
| HIS             | [Hospital information systems](https://en.wikipedia.org/wiki/Hospital_information_system)                                                                                                       |
| IOD             | [Information Object Definition](http://dicom.nema.org/medical/dicom/current/output/chtml/part02/sect_A.3.4.html)                                                                                |
| PACS            | [Picture Archiving and Communications System](https://en.wikipedia.org/wiki/Picture_archiving_and_communication_system)                                                                         |
| SCP             | [Service Class Provider](http://dicom.nema.org/medical/dicom/current/output/chtml/part02/sect_A.3.4.html)                                                                                       |
| SCU             | [Service Class User](http://dicom.nema.org/medical/dicom/current/output/chtml/part02/sect_A.3.4.html)                                                                                           |
| Transfer Syntax | [Transfer Syntax](http://dicom.nema.org/medical/dicom/current/output/chtml/part02/sect_A.3.4.html)                                                                                              |
| UID             | [Unique Identifier](http://dicom.nema.org/medical/dicom/current/output/chtml/part02/sect_A.3.4.html)                                                                                            |
| VR              | [Value Representation](http://dicom.nema.org/medical/dicom/current/output/chtml/part02/sect_A.3.4.html)                                                                                         |

### Reference Documents

- [Informatics Gateway Requirements](informatics-gateway-requirements.md)

---

## ​Architecture Details

The goal is to provide an easy integration path with hospital information systems and allow them to integrate image processing/inference workflows using MIG.

MONAI Deploy Informatics Gateway is designed to follow communication and data standards in the healthcare industry to enable interoperability between HIS and the MONAI Deploy platform. Such standards are, but not limited to, DICOM, DICOMweb and FHIR.

### API Surface Area
MIG provides the following services for interacting with external devices and/or services.

#### DICOM

- **DICOM SCP** to listen for incoming verification and store requests.
- **DICOM SCU** to export DICOM dataset to designated DICOM devices.

#### DICOM web

- **DICOMweb Client** to query, retrieve, store DICOM dataset against configured DICOMweb servers.

#### FHIR

- **FHIR Client** to interact with a FHIR server and its available FHIR resources.

#### Others

- **ACR-DSI API** to provide a standard for AI model inference in a clinical workflow.
- **Management APIs** to provide functionalities of configuring MIG during runtime.


---

## Design

### DICOM SCP Service

![DICOM SCP Sequence Diagram](diagrams/mig-scp.png)

MIG's (the system) Storage SCP provides DICOM C-ECHO and C-STORE services to interface with other DICOM devices, such as PACS. The system allows users to configure an (one) AE Title where the AET allows one or more concurrent incoming associations at a given time. Associations are rejected if more than configured associations are in session.

Upon accepting an incoming association, an unique identifer (UUID) is generated for data flow correlation purposes.

If enabled, the AET verifies the calling AET by validating the source IP address and the calling AE Title against whitelisted sources.

Accepted DICOM instances are uploaded to MONAI Workload Manager (MWM) for further processing.

Each request made to the MWM includes includes the unique identifier (UUID) generated when the association was accepted.

The C-ECHO (verification) service can be enabled or disabled based on configuration.

#### Association Policies

- MIG SCP AET accepts associations but does not initiate associations.
- MIG Storage SCP, by default, accepts up `25` (configurable) concurrent associations.
- MIG Storage SCP accepts associations when storage space usage is less than the configured watermark and the available storage space is above the configured reserved storage size.
- Asynchronous mode is not supported. All operations are performed synchronously.
- The Implementation Class UID is `1.3.6.1.4.1.30071.8` and the Implementation Version Name is `fo-dicom {major}.{minor}.{build}`.

#### Security Profiles

MIG Storage SCP does not conform to any defined DICOM Security Profiles.
It is assumed that the product is used within a secured environment that uses a Firewall, Router Protection, VPN, etc.

MIG Storage SCP service can be configured to accept all incoming association requests or check against a whitelisted AET and its:

- Called AE Title
- Calling AE Title
- Calling IP Address

#### Retry Logic

The system would retry the following actions upon failure. Values can be overridden in the configuration file.

| Action       | Retry Delay             | Maximum Retries |
| ------------ | ----------------------- | --------------- |
| Save to disk | Sliding: 250ms - 1000ms | 3               |
| Notify MWM   | Sliding: 250ms - 1000ms | 3               |

---

### DICOM SCU Service

![Export Sequence Diagram](diagrams/mig-export.png)

MIG's (the system) Storage SCU provides DICOM Storage Service to interface with other medical devices, such as PACS, to enable exporting of any DICOM artifacts produced by the applications.

The SCU AE Title can be configured by the users.

MIG DICOM Storage SCU initiates a push of DICOM objects or a C-STORE request to the Remote DICOM Storage SCP. The system shall allow multiple Remote (destination) SCPs to be configured.

Each Remote DICOM Storage SCP must be uniquely named so they can be referenced by MWM sinks.

C-STORE SCU stops all processing when storage space usage is less than the configured watermark and the available storage space is above the configured reserved storage size.

#### SOP Classes (Transfer) Supported & Transfer Syntax

The DICOM Store SCU service shall support all SOP classes of the Storage Service Class.

The DICOM Store SCU service shall transfer a DICOM object as-is using stored Transfer Syntax without the support of compression, decompression or Transfer Syntax conversion.

#### Association Policies

- MIG DICOM Storage SCU initiates associations but does not accept associations.
- MIG DICOM Storage SCU allows, by default, 2 (configurable) SCU instances simultaneously.
- Asynchronous mode is not supported. All operations are performed synchronously.
- The Implementation Class UID is `1.3.6.1.4.1.30071.8` and the Implementation Version Name is `fo-dicom {major}.{minor}.{build}`.

#### Security Profiles

Not applicable.

#### Retry Logic

The system would retry the following actions upon failure. Values can be overridden in configuration file.

| Action | Retry Delay             | Maximum Retries |
| ------ | ----------------------- | --------------- |
| Export | Sliding: 250ms - 1000ms | 3               |

---

### DICOMweb Client

The DICOMweb client enable querying, retrieving and storing of DICOM objects to DICOMweb enabled services.

#### WADO Client APIs

WADO (Web Access to DICOM Objects) client contains a set of APIs defined by the DICOM standard. MIG support the following WADO APIs:

- `GET /studies/{study}` Retrieve Study
- `GET /studies/{study}/metadata` Retrieve Study metadata
- `GET /studies/{study}/series/{series}` Retrieve Series
- `GET /studies/{study}/series/{series}/metadata` Retrieve Series metadata
- `GET /studies/{study}/series/{series}/instances/{instance}` Retrieve Instance
- `GET /studies/{study}/series/{series}/instances/{instance}/metadata` Retrieve Instance metadata
- `GET /studies/{study}/series/{series}/instances/{instance}/bulkdata/{bulkdata}` Retrieve Bulkdata

#### QIDO Client APIs

QIDO (Query based on ID for DICOM Objects) client contains a set of APIs defined by the DICOM standard and MIG supports the following QIDO APIs:

- `GET /studies` Search for Studies

#### STOW Client APIs

STOW (Store Over the WEb) client enables storing DICOM instances to a DICOMweb server.
MIG supports the following STOW APIs:

- `POST /studies` Store instances
- `POST /studies/{study}` Store instances

#### Retry Logic

The DICOMweb client does not perform any retries. However, the Data Retrieval component that utilizes the DICOMweb client would handle retries.

| Action | Retry Delay | Maximum Retries |
| ------ | ----------- | --------------- |
| \*     | None        | None            |

---

### FHIR Client

MIG (the system) provides a FHIR client to exchange FHIR resources with FHIR enabled services.

In order to retrieve a FHIR resource, users must specify the type and ID of a resource.
The system retrieves FHIR resources, by default, in JSON format. However, user may configure the system to

MIG also allows user to export FHIR resources to designated FHIR services.

_Limitations_: The FHIR client works in conjunction with ACR API. In order to retrieve or export any FHIR resources, users must explicitly specify the endpoints of each FHIR service.

#### Retry Logic

The FHIR client does not perform any retries. However, the Data Retrieval component that utilizes the FHIR client would handle retries.

| Action | Retry Delay | Maximum Retries |
| ------ | ----------- | --------------- |
| \*     | None        | None            |

---

### Logging

MONAI Deploy Informatics Gateway logs all actions it performs and tries to associate each action with an unique identifier for traceability.

Different log levels are used and are defined in [LogLevel Enum](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel).

The entire DIMSE dataset (for SCP) may be logged but no anonymization would be performed and therefore this feature is, by default, disabled.

---

### ACR-DSI API (future)

![ACR Sequence Diagram](diagrams/mig-acr.png)

The [ACR-DSI API](https://www.acrdsi.org/-/media/DSI/Files/ACR-DSI-Model-API.pdf) is proposed by the American College of Radiology’s Data Science Institute.  The implementation of the API specs may be different from the original proposal, please refer to the API documentation for details.

The transaction ID supplied in the API call is used when notifying MWM of the dataset for data flow trace purposes. Therefore, the transaction ID must be unique.

The following APIs are supported to interact with the ACR-DSI API:

#### Inference API

Initiates a new inference requesting using ACR-DSI drafted API. This API retrieves the specified DICOM dataset and/or FHIR dataset and

##### URL

`/inference`

##### Meethod

`POST`

##### Data Params

Refer to [ACR Inference API specs]() for detailed information.

##### Success Response

- Code: `200`

  Content: `{ transactionId: "...", correlationId: "..." }`

- Code: `422`

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with validation errors.

- Code: `500`

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with error details.

#### Inference Status API

##### URL

`/inference/{transactionId}`

##### Meethod

`GET`

##### Success Response

- Code: `200`

  Content: TBD

- Code: `500`

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with error details.

---

### Health API

#### URL

`/health/live`
`/health/ready`

#### Meethod

`GET`

#### Success Response

- Code: `200`
  Content: `{ "status": "UP", "checks": { "SCP": { "status": "UP" } ... } }`
- Code: `503`

  Content: `{ "status": "OUT_OF_SERVICE", "checks": { "SCP": { "status": "UP" } } }`

---

### List Source AET Config API

Returns a list of source AE Titles configured on the Informatics Gateway.

#### URL

`/config/source`

#### Method

`GET`

#### Success Response

- Code: `200`
  Content: `[{"name": "USEAST", "hostIp": "10.20.3.4", "aeTitle": "MYPACS" },...]`
- Code: `500`: Server error.

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.

### Add Source AET Config API

Adds a new source AE Title.

#### URL

`/config/source`

#### Meethod

`POST`

##### Data Params

```
{
    "name": "USEAST",
	"hostIp": "10.20.3.4",
	"aeTitle": "MYPACS"
}
```

#### Success Response

- Code: `201`: AE Title created successfully.
  Content: `{"name": "USEAST", "hostIp": "10.20.3.4", "aeTitle": "MYPACS" }`
- Code: `400`: Validation error.

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with validation error details.

- Code: `500`: Server error.

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.

### Delete Source AET Config API

Deletes a source AE Title.

#### URL

`/config/source/{name}`

#### Meethod

`DELETE`

#### Success Response

- Code: `201`: AE Title deleted.
  Content: `{"name": "USEAST", "hostIp": "10.20.3.4", "aeTitle": "MYPACS" }`
- Code: `404`: AE Title not found.

  Content: None

- Code: `500`: Server error.

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.

---

### List Destination AET Config API

List all destination AE Titles configured on the system.

#### URL

`/config/destination`

#### Meethod

`GET`

#### Success Response

- Code: `200`
  Content: `[{"name": "USEAST", "hostIp": "10.20.3.4", port: 104, "aeTitle": "MYPACS" },...]`
- Code: `500`: Server error.

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.

### Add Destination AET Config API

Adds a new destination AE Title.

#### URL

`/config/destination`

#### Meethod

`POST`

##### Data Params

```
{
    "name": "USEAST",
	"hostIp": "10.20.3.4",
    "port": 104,
	"aeTitle": "MYPACS"
}
```

#### Success Response

- Code: `201`: AE Title created successfully.
  Content: `{"name": "USEAST", "hostIp": "10.20.3.4", "port": 104, "aeTitle": "MYPACS" }`
- Code: `400`: Validation error.

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with validation error details.

- Code: `500`: Server error.

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.

### Delete Destination AET Config API

Deletes a destination AE Title.

#### URL

`/config/destination/{name}`

#### Meethod

`DELETE`

#### Success Response

- Code: `201`: AE Title deleted.
  Content: `{"name": "USEAST", "hostIp": "10.20.3.4", "port": 104, "aeTitle": "MYPACS" }`
- Code: `404`: AE Title not found.

  Content: None

- Code: `500`: Server error.

  Content: A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.
