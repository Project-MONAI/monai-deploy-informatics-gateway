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

# Configuration APIs

The _configuration_ endpoint provides the following APIs to configure the Informatics Gateway.

## GET /config/ae

Returns a list of MONAI Deploy SCP Application Entity Titles configured on the Informatics Gateway.

### Parameters

N/A

### Responses

Response Content Type: JSON - Array of [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Titles retrieved successfully.                                                                                                       |
| 404  | AE Title not found.                                                                                                                     |
| 409  | Entity already exists with the same name or AE Title.                                                                                   |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/ae'
```

### Example Response

```json
[
  {
    "name": "brain-tumor",
    "aeTitle": "BrainTumorModel",
    "workflows": ["brain-tumor", "b75cd27a-068a-4f9c-b3da-e5d4ea08c55a"],
    "grouping": "0020,000D",
    "timeout": 5,
    "ignoredSopClasses": ["1.2.840.10008.5.1.4.1.1.1.1"],
    "allowedSopClasses": ["1.2.840.10008.5.1.4.1.1.1.2"]
  },
  {
    "name": "liver-seg",
    "aeTitle": "LIVERSEG",
    "grouping": "0020,000D",
    "timeout": 5,
    "workflows": []
  }
]
```

---

## GET /config/ae/{name}

Returns the configuration of the specified MONAI SCP AE Title.

### Parameters

| Name | Type   | Description                           |
| ---- | ------ | ------------------------------------- |
| name | string | The _name_ of the AE to be retrieved. |

### Responses

Response Content Type: JSON - [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | Configuration retrieved successfully.                                                                                                   |
| 404  | Named AE not found.                                                                                                         |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/ae/brain-tumor'
```

### Example Response

```json
{
  "name": "brain-tumor",
  "aeTitle": "BrainTumorModel",
  "grouping": "0020,000D",
  "timeout": 5,
  "workflows": ["brain-tumor", "b75cd27a-068a-4f9c-b3da-e5d4ea08c55a"]
}
```

---

## POST /config/ae

Creates a new MONAI SCP Application Entity to accept DICOM instances.

> [!Note]
> The MONAI SCP AE Title must be unique.

> [!Note]
> The DICOM tag used for `grouping` can be either a Study Instance UID (0020,000D) or Series Instance UID (0020,000E).
> The default is set to a Study Instance UID (0020,000D) if not specified.

> [!Note]
> `timeout` is the number of seconds the AE Title will wait between each instance before assembling a payload and publishing
> a workflow request. We recommend calculating this value based on the network speed and the maximum size of each
> DICOM instance.

### Parameters

See the [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity)
class definition for details.

### Responses

Response Content Type: JSON - [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity).

| Code | Description                                                                                                                                 |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| 201  | AE Title created successfully.                                                                                                              |
| 400  | Validation error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |
| 409  | Entity already exists with the same name or entity already exists with the same AE Title and port combination.                              |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.     |

### Example Request

```bash
curl --location --request POST 'http://localhost:5000/config/ae/' \
    --header 'Content-Type: application/json' \
    --data-raw '{
            "name": "breast-tumor",
            "aeTitle": "BREASTV1",
            "timeout": 5,
            "workflows": [
                "3f6a08a1-0dea-44e9-ab82-1ff1adf43a8e"
            ]
        }
    }'
```

### Example Response

```json
{
  "name": "breast-tumor",
  "aeTitle": "BREASTV1",
  "workflows": ["3f6a08a1-0dea-44e9-ab82-1ff1adf43a8e"]
}
```

---

## PUT /config/ae

Updates an existing MONAI SCP Application Entity.

> [!Note]
> The MONAI SCP AE Title cannot be changed.

> [!Note]
> The DICOM tag used for `grouping` can be either a Study Instance UID (0020,000D) or Series Instance UID (0020,000E).
> The default is set to a Study Instance UID (0020,000D) if not specified.

> [!Note]
> `timeout` is the number of seconds the AE Title will wait between each instance before assembling a payload and publishing
> a workflow request. We recommend calculating this value based on the network speed and the maximum size of each
> DICOM instance.

### Parameters

See the [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity)
class definition for details.

### Responses

Response Content Type: JSON - [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity).

| Code | Description                                                                                                                                 |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Title updated successfully.                                                                                                              |
| 400  | Validation error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |
| 404  | Named MONAI AE not found.                                                                                                                      |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.     |

### Example Request

```bash
curl --location --request PUT 'http://localhost:5000/config/ae/' \
    --header 'Content-Type: application/json' \
    --data-raw '{
            "name": "breast-tumor",
            "timeout": 3,
            "workflows": [
                "3f6a08a1-0dea-44e9-ab82-1ff1adf43a8e"
            ]
        }
    }'
```

### Example Response

```json
{
  "name": "breast-tumor",
  "aeTitle": "BREASTV1",
  "workflows": ["3f6a08a1-0dea-44e9-ab82-1ff1adf43a8e"],
  "timeout": 3
}
```

---
## DELETE /config/ae/{name}

Deletes the specified MONAI SCP Application Entity.

### Parameters

| Name | Type   | Description                               |
| ---- | ------ | ----------------------------------------- |
| name | string | The _name_ of the AE Title to be deleted. |

### Responses

Response Content Type: JSON - [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity).

| Code | Description                                                                                                                              |
| ---- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Title deleted.                                                                                                                        |
| 404  | Named MONAI AE not found.                                                                                                                      |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.  |

### Example Request

```bash
curl --location --request DELETE 'http://localhost:5000/config/ae/breast-tumor'
```

### Example Response

```json
{
  "name": "breast-tumor",
  "aeTitle": "BREASTV1",
  "workflows": ["3f6a08a1-0dea-44e9-ab82-1ff1adf43a8e"]
}
```

---

## GET /config/source

Returns a list of calling (source) AE Titles configured on the Informatics Gateway.

### Parameters

N/A

### Responses

Response Content Type: JSON - Array of [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity).

| Code | Description                                                                                                                              |
| ---- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Titles retrieved successfully.                                                                                                        |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.  |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/source'
```

### Example Response

```json
[
  {
    "name": "USEAST",
    "aeTitle": "PACSUSEAST",
    "hostIp": "10.20.3.4"
  },
  {
    "name": "USWEST",
    "aeTitle": "PACSUSWEST",
    "hostIp": "10.50.3.4"
  }
]
```

---

## GET /config/source/{name}

Returns configurations for the specified calling (source) AET.

### Parameters

| Name | Type   | Description                                |
| ---- | ------ | ------------------------------------------ |
| name | string | The _name_ of an AE Title to be retrieved. |

### Responses

Response Content Type: JSON - [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Titles retrieved successfully.                                                                                                       |
| 404  | Named source not found.                                                                                                                    |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/source/USEAST'
```

### Example Response

```json
{
  "name": "USEAST",
  "aeTitle": "PACSUSEAST",
  "hostIp": "10.20.3.4"
}
```

---

## POST /config/source

Adds a new calling (source) AE Title to the Informatics Gateway to allow DICOM instances from the specified IP address and AE Title.

### Parameters

See the [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity)
class definition for details.

### Responses

Response Content Type: JSON - [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 201  | AE Title created successfully.                                                                                                          |
| 400  | Validation error.                                                                                                                       |
| 409  | Entity already exists with the same name or entity already exists with the same AE Title, host/IP address and port combination.         |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request POST 'http://localhost:5000/config/source' \
    --header 'Content-Type: application/json' \
    --data-raw '{
        "name": "USEAST",
        "hostIp": "10.20.3.4",
        "aeTitle": "PACSUSEAST"
    }'
```

### Example Response

```json
{
  "name": "USEAST",
  "aeTitle": "PACSUSEAST",
  "hostIp": "10.20.3.4"
}
```

---

## PUT /config/source

Updates an existing calling (source) AE Title.

### Parameters

See the [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity)
class definition for details.

### Responses

Response Content Type: JSON - [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity).

| Code | Description                                                                                                                                              |
| ---- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Title updated successfully.                                                                                                                           |
| 400  | Validation error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with details of the validation errors . |
| 404  | DICOM source cannot be found.                                                                                                                            |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.                  |

### Example Request

```bash
curl --location --request PUT 'http://localhost:5000/config/source' \
    --header 'Content-Type: application/json' \
    --data-raw '{
        "name": "USEAST",
        "hostIp": "10.20.3.4",
        "aeTitle": "PACSUSEAST"
    }'
```

### Example Response

```json
{
  "name": "USEAST",
  "aeTitle": "PACSUSEAST",
  "hostIp": "10.20.3.4"
}
```

---

## DELETE /config/source/{name}

Deletes the specified calling (Source) AE Title to stop accepting requests from it.

### Parameters

| Name | Type   | Description                               |
| ---- | ------ | ----------------------------------------- |
| name | string | The _name_ of the AE Title to be deleted. |

### Responses

Response Content Type: JSON - [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Title deleted.                                                                                                                       |
| 404  | Named source not found.                                                                                                                     |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request DELETE 'http://localhost:5000/config/source/USEAST'
```

### Example Response

```json
{
  "name": "USEAST",
  "aeTitle": "PACSUSEAST",
  "hostIp": "10.20.3.4"
}
```

---

## GET /config/destination

Returns a list of destination AE titles configured on the system.

### Parameters

N/A

### Responses

Response Content Type: JSON - Array of [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Titles retrieved successfully.                                                                                                       |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/destination'
```

### Example Response

```json
[
  {
    "port": 104,
    "name": "USEAST",
    "aeTitle": "PACSUSEAST",
    "hostIp": "10.20.3.4"
  },
  {
    "port": 104,
    "name": "USWEST",
    "aeTitle": "PACSUSWEST",
    "hostIp": "10.50.3.4"
  }
]
```

---

## GET /config/destination/{name}

Retrieves the destination AE Title with the specified name.

### Parameters

| Name | Type   | Description                                |
| ---- | ------ | ------------------------------------------ |
| name | string | The _name_ of theAE Title to be retrieved. |

### Responses

Response Content Type: JSON - [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Titles retrieved successfully.                                                                                                       |
| 404  | AE Titles not found.                                                                                                                    |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/destination/USEAST'
```

### Example Response

```json
{
  "port": 104,
  "name": "USEAST",
  "aeTitle": "PACSUSEAST",
  "hostIp": "10.20.3.4"
}
```

---

## GET /config/destination/cecho/{name}

Performs a DICOM C-Echo request to the named destination on behalf of `MONAISCU`.

### Parameters

| Name | Type   | Description                               |
| ---- | ------ | ----------------------------------------- |
| name | string | The _name_ of the AE Title to be deleted. |

### Responses


| Code | Description                                                                                                                               |
| ---- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | C-ECHO performed successfully.                                                                                                            |
| 404  | Named destination not found.                                                                                                                       |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.   |
| 502  | C-ECHO failure. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request DELETE 'http://localhost:5000/config/destination/cecho/USEAST'
```

---

## PUT /config/destination

Updates an existing DICOM destination.

### Parameters

See the [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity)
class definition for details.

### Responses

Response Content Type: JSON - [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity).

| Code | Description                                                                                                                                              |
| ---- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | DICOM destination updated successfully.                                                                                                                  |
| 400  | Validation error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with details of the validation errors . |
| 404  | DICOM destination cannot be found.                                                                                                                       |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.                  |

### Example Request

```bash
curl --location --request PUT 'http://localhost:5000/config/destination' \
    --header 'Content-Type: application/json' \
    --data-raw '{
        "name": "USEAST",
        "hostIp": "10.20.3.4",
        "port": 104,
        "aeTitle": "PACSUSEAST"
    }'
```

### Example Response

```json
{
  "port": 104,
  "name": "USEAST",
  "aeTitle": "PACSUSEAST",
  "hostIp": "10.20.3.4"
}
```

---

## POST /config/destination

Adds a new DICOM destination AET for exporting results to.

### Parameters

See the [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity)
class definition for details.

### Responses

Response Content Type: JSON - [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 201  | AE Title created successfully.                                                                                                          |
| 400  | Validation error.                                                                                                                       |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request POST 'http://localhost:5000/config/destination' \
    --header 'Content-Type: application/json' \
    --data-raw '{
        "name": "USEAST",
        "hostIp": "10.20.3.4",
        "port": 104,
        "aeTitle": "PACSUSEAST"
    }'
```

### Example Response

```json
{
  "port": 104,
  "name": "USEAST",
  "aeTitle": "PACSUSEAST",
  "hostIp": "10.20.3.4"
}
```

---

## DELETE /config/destination/{name}

Deletes a Destination AE Title.

### Parameters

| Name | Type   | Description                               |
| ---- | ------ | ----------------------------------------- |
| name | string | The _name_ of the AE Title to be deleted. |

### Responses

Response Content Type: JSON - [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity).

| Code | Description                                                                                                                             |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 200  | AE Title deleted.                                                                                                                       |
| 404  | Named destination not found.                                                                                                                     |
| 500  | Server error. The response will be a [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request DELETE 'http://localhost:5000/config/destination/USEAST'
```

### Example Response

```json
{
  "port": 104,
  "name": "USEAST",
  "aeTitle": "PACSUSEAST",
  "hostIp": "10.20.3.4"
}
```
