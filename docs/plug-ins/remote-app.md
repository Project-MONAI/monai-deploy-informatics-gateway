<!--
  ~ Copyright 2023 MONAI Consortium
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

# Remote App Execution Plug-ins

The **Remote App Execution Plug-ins** allow the users to configure a set of DICOM metadata to be replaced with dummy data before
being exported using the `DicomDeidentifier` plug-in. The original data is stored in the database; when the data returns
via DICOM DIMSE or DICOMWeb, the data can be restored using the `DicomReidentifier` plug-in.

## Supported Data Types

-   DICOM

## Configuration

One or more DICOM tags may be configured in the `appsettings.json` file. For example, the following snippet will replace the
`AccessionNumber`, `StudyDescription`, and `SeriesDescription` tags.

```json
{
    "InformaticsGateway": {
        "plugins": {
            "remoteApp": {
                "ReplaceTags": "AccessionNumber, StudyDescription, SeriesDescription"
            }
        }
    }
}
```

Refer to [NEMA](https://dicom.nema.org/medical/dicom/current/output/chtml/part06/chapter_6.html) for a complete list of DICOM tags
and use the value from the **Keyword** column.

> [!Note]
> `StudyInstanceUID`, `SeriesInstanceUID` and `SOPInstanceUID` are always replaced and tracked to ensure the same
> studies and series get the same UIDs.

> [!Important]
> Only top-level DICOM metadata can be replaced at this time.

## Fully Qualified Assembly Names

The following plug-ins are available:

| Name                | Fully Qualified Assembly Name                                                                                                              |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `DicomDeidentifier` | `Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.DicomDeidentifier, Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution` |
| `DicomReidentifier` | `Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.DicomReidentifier, Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution` |
