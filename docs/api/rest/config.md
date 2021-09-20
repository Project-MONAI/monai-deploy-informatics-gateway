# Configuration APIs

The _configuration_ endpoint provide the following APIs to configured the Informatics Gateway.

## GET /config/monaiaetitle

Returns a list of MONAI Deploy SCP Application Entity Titles configured on the Informatics Gateway.

### Parameters

N/A

### Responses

Response Content Type: JSON - Array of [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | AE Titles retrieved successfully.                                                                                  |
| 404  | AE Title not found.                                                                                                |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/monaiaetitle'
```

### Example Response

```json
[
  {
    "name": "brain-tummor",
    "aeTitle": "BrainTumorModel",
    "applications": ["brain-tumor", "b75cd27a-068a-4f9c-b3da-e5d4ea08c55a"]
  },
  {
    "name": "liver-seg",
    "aeTitle": "LIVERSEG",
    "applications": []
  }
]
```

---

## GET /config/monaiaetitle/{name}

Returns configurations for the specified MONAI SCP AE Title.

### Parameters

| Name | Type   | Description                           |
| ---- | ------ | ------------------------------------- |
| name | string | the _name_ of the AE to be retrieved. |

### Responses

Response Content Type: JSON - [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | AE Titles retrieved successfully.                                                                                  |
| 404  | AE Titles not found.                                                                                               |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/monaiaetitle/brain-tummor'
```

### Example Response

```json
{
  "name": "brain-tummor",
  "aeTitle": "BrainTumorModel",
  "applications": ["brain-tumor", "b75cd27a-068a-4f9c-b3da-e5d4ea08c55a"]
}
```

---

## POST /config/monaiaetitle

Creates a new MONAI SCP Application Entity to accept DICOM instances.

> [!Note]
> The MONAI SCP AE Title must be unique.

### Parameters

Please see the [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity)
class definition for details.

### Responses

Response Content Type: JSON - [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity).

| Code | Description                                                                                                               |
| ---- | ------------------------------------------------------------------------------------------------------------------------- |
| 201  | AE Title created successfully.                                                                                            |
| 400  | Validation error.A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with validation error details. |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details.        |

### Example Request

```bash
curl --location --request POST 'http://localhost:5000/config/monaiaetitle/' \
--header 'Content-Type: application/json' \
--data-raw '{
        "name": "breast-tumor",
        "aeTitle": "BREASTV1",
        "application": [
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
  "applications": ["3f6a08a1-0dea-44e9-ab82-1ff1adf43a8e"]
}
```

---

## DELETE /config/monaiaetitle/{name}

Deletes the specified MONAI SCP Application Entity.

### Parameters

| Name | Type   | Description                               |
| ---- | ------ | ----------------------------------------- |
| name | string | the _name_ of the AE Title to be deleted. |

### Responses

Response Content Type: JSON - [MonaiApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | AE Title deleted.                                                                                                  |
| 404  | AE Title not found.                                                                                                |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request DELETE 'http://localhost:5000/config/monaiaetitle/breast-tumor'
```

### Example Response

```json
{
  "name": "breast-tumor",
  "aeTitle": "BREASTV1",
  "applications": ["3f6a08a1-0dea-44e9-ab82-1ff1adf43a8e"]
}
```

---

## GET /config/sourceaetitle

Returns a list of calling (source) AE Titles configured on the Informatics Gateway.

### Parameters

N/A

### Responses

Response Content Type: JSON - Array of [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | AE Titles retrieved successfully.                                                                                  |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/sourceaetitle'
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

## GET /config/sourceaetitle/{name}

Returns configurations for the specified calling (source) AET.

### Parameters

| Name | Type   | Description                                |
| ---- | ------ | ------------------------------------------ |
| name | string | the _name_ of an AE Title to be retrieved. |

### Responses

Response Content Type: JSON - [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | AE Titles retrieved successfully.                                                                                  |
| 404  | AE Titles not found.                                                                                               |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/sourceaetitle/USEAST'
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

## POST /config/sourceaetitle

Adds a new calling (source) AE Title to Informatics Gateway to allow DICOM instances from the IP address & AE Title specified.

### Parameters

Please see the [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity)
class definition for details.

### Responses

Response Content Type: JSON - [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 201  | AE Title created successfully.                                                                                     |
| 400  | Validation error.                                                                                                  |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request POST 'http://localhost:5000/config/sourceaetitle' \
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

## DELETE /config/sourceaetitle/{name}

Deletes the specified calling (Source) AE Title to stop accepting requests from it.

### Parameters

| Name | Type   | Description                               |
| ---- | ------ | ----------------------------------------- |
| name | string | the _name_ of the AE Title to be deleted. |

### Responses

Response Content Type: JSON - [SourceApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | AE Title deleted.                                                                                                  |
| 404  | AE Title not found.                                                                                                |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request DELETE 'http://localhost:5000/config/sourceaetitle/USEAST'
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

## GET /config/destinationaetitle

Returns a list of destination AE titles configured on the system.

### Parameters

N/A

### Responses

Response Content Type: JSON - Array of [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | AE Titles retrieved successfully.                                                                                  |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/destinationaetitle'
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

## GET /config/destinationaetitle/{name}

Retrieves the named destination AE Title.

### Parameters

| Name | Type   | Description                             |
| ---- | ------ | --------------------------------------- |
| name | string | the _name_ of AE Title to be retrieved. |

### Responses

Response Content Type: JSON - [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | AE Titles retrieved successfully.                                                                                  |
| 404  | AE Titles not found.                                                                                               |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request GET 'http://localhost:5000/config/destinationaetitle/USEAST'
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

## POST /config/destinationaetitle

Adds a new DICOM destination AET to allow results to be exported to.

### Parameters

Please see the [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity)
class definition for details.

### Responses

Response Content Type: JSON - [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 201  | AE Title created successfully.                                                                                     |
| 400  | Validation error.                                                                                                  |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request POST 'http://localhost:5000/config/destinationaetitle' \
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

## DELETE /config/destinationaetitle/{name}

Deletes a Destination AE Title.

### Parameters

| Name | Type   | Description                               |
| ---- | ------ | ----------------------------------------- |
| name | string | the _name_ of the AE Title to be deleted. |

### Responses

Response Content Type: JSON - [DestinationApplicationEntity](xref:Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity).

| Code | Description                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------ |
| 200  | AE Title deleted.                                                                                                  |
| 404  | AE Title not found.                                                                                                |
| 500  | Server error. A [Problem details](https://datatracker.ietf.org/doc/html/rfc7807) object with server error details. |

### Example Request

```bash
curl --location --request DELETE 'http://localhost:5000/config/monaiaetitle/USEAST'
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
