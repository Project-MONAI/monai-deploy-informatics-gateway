<!--
SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
SPDX-License-Identifier: Apache License 2.0
-->

# Health APIs

The _health_ endpoint provides the following APIs to get statues of the internals of Informatics Gateway.

## GET /health/status

MONAI Informatics Gateway service status:

- Active DICOM DIMSE associations
- Internal service status

### Parameters

N/A

### Responses

Response Content Type: JSON - [HealthStatusResponse](xref:Monai.Deploy.InformaticsGateway.Api.Rest.HealthStatusResponse).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | Status is available.                                                                                               |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/health/status'
```

### Example Response

```json
{
  "activeDimseConnections": 8,
  "services": {
    "Space Reclaimer Service": "Running",
    "DICOM SCP Service": "Running",
    "DICOMweb Export Service": "Running",
    "DICOM Export Service": "Running",
    "Data Retrieval Service": "Running",
    "Workload Manager Notification Service": "Running"
  }
}
```

---

## GET /health/ready & GET /health/live

MONAI Deploy Informatics Gateway service readiness and liveness.

### Parameters

N/A

### Responses

Response Content Type: JSON

- `Health`: All services are running.
- `Unhealthy`: One or more services have stopped or crashed.

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | Service is healthy.                                                                                                |
| 503  | Service is unhealthy.                                                                                              |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/health/live'
```

### Example Response

```json
"Healthy"
```
