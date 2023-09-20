<!--
  ~ Copyright 2021-2023 MONAI Consortium
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

# DICOM Association information

The `/dai' endpoint is for retrieving a list of information regarding dicom
associations.

## GET /dai/

#### Query Parameters

| Name       | Type     | Description                                 |
|------------|----------|---------------------------------------------|
| startTime  | DateTime | (Optional) Start date to query from.        |
| endTime    | DateTime | (Optional) End date to query from.          |
| pageNumber | Number   | (Optional) Page number to query.(default 0) |
| pageSize   | Number   | (Optional) Page size of query.              |

Max & Defaults for PageSize can be set in appSettings.

```json
"endpointSettings": {
  "defaultPageSize": number,
  "maxPageSize": number
}
```

Endpoint returns a paged result for example

```json
{
  "PageNumber": 1,
  "PageSize": 10,
  "FirstPage": "/payload?pageNumber=1&pageSize=10",
  "LastPage": "/payload?pageNumber=1&pageSize=10",
  "TotalPages": 1,
  "TotalRecords": 3,
  "NextPage": null,
  "PreviousPage": null,
  "Data": [...]
  "Succeeded": true,
  "Errors": null,
  "Message": null
}
```
