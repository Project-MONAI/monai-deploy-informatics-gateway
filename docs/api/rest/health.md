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

# Health APIs

The _health_ endpoint provides the following APIs to get the status of the internals of the Informatics Gateway.



## GET /health/

Returns the MONAI Deploy Informatics Gateway service readiness and liveness.

### Parameters

N/A

### Responses

Response Content Type: JSON

- `Healthy`: All services are running.
- `Unhealthy`: One or more services have stopped or crashed.

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | Service is healthy.                                                                                                                     |
| 503  | Service is unhealthy.                                                                                                                   |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/health'
```

### Example Response

```json
{
  "status": "Healthy",
  "checks": [
    {
      "check": "Informatics Gateway Services",
      "result": "Healthy"
    },
    {
      "check": "InformaticsGatewayContext",
      "result": "Healthy"
    },
    {
      "check": "minio",
      "result": "Healthy"
    },
    {
      "check": "Rabbit MQ Subscriber",
      "result": "Healthy"
    },
    {
      "check": "Rabbit MQ Publisher",
      "result": "Healthy"
    }
  ]
}
```

---

## GET /health/status

Returns the MONAI Informatics Gateway service status:

- Active DICOM DIMSE associations
- Internal service status

### Parameters

N/A

### Responses

Response Content Type: JSON - [HealthStatusResponse](xref:Monai.Deploy.InformaticsGateway.Api.Rest.HealthStatusResponse).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | Status is available.                                                                                                                    |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

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
