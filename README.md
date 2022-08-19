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

<p align="center">
  <img src="https://raw.githubusercontent.com/Project-MONAI/MONAI/dev/docs/images/MONAI-logo-color.png" width="50%" alt='project-monai'>
</p>

💡 If you want to know more about MONAI Deploy WG vision, overall structure, and guidelines, please read [MONAI Deploy](https://github.com/Project-MONAI/monai-deploy) main repo first.


# MONAI Deploy Informatics Gateway

[![License](https://img.shields.io/badge/license-Apache%202.0-green.svg)](LICENSE)
[![codecov](https://codecov.io/gh/Project-MONAI/monai-deploy-informatics-gateway/branch/main/graph/badge.svg?token=34S8VI0XGD)](https://codecov.io/gh/Project-MONAI/monai-deploy-informatics-gateway)
[![ci](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions/workflows/ci.yml/badge.svg)](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions/workflows/ci.yml)

MONAI Deploy Informatics Gateway (MIG) handles the first and last step in a clinical data pipeline, the Input, and Output (I/O).

MIG uses standard protocols like DICOM and FHIR. It stores studies and resources from the medical record system as payloads. It then notifies the rest of the MONAI Deploy system, specifically the MONAI Deploy Workflow Manager, that data is ready to be processed.

After inference completes, MIG receives notifications for exporting the results to the proper consumers, usually PACS or viewers for visualization, VNAs for storage, and EHRs (Electronic Healthcare Records).

## Services

For a list of services hosted and supported by the Informatics Gateway, please refer to the [User Guide](./docs/index.md) section.

## Installation

Please refer to the latest [user guide](./docs/setup/setup.md) for installation instructions.

## Getting started

### Runtime Requirements

* Docker 20.10.7 or later

### Development Requirements

* [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

During development, change any settings inside the `appsettings.Development.json` file.
First, export the following environment variable before executing `dotnet run`:

#### Bash 

```bash
export DOTNET_ENVIRONMENT=Development
```
#### Powershell

```powershell
$env:DOTNET_ENVIRONMENT="Development"
```

### Building MONAI Deploy Informatics Gateway

```bash
dotnet build
```

## Contributing
For guidance on making a contribution, see the [contributing guidelines](https://github.com/Project-MONAI/monai-deploy/blob/main/CONTRIBUTING.md).

## Community
To participate, please join the MONAI Deploy App SDK weekly meetings on the [calendar](https://calendar.google.com/calendar/u/0/embed?src=c_954820qfk2pdbge9ofnj5pnt0g@group.calendar.google.com&ctz=America/New_York) and review the [meeting notes](https://docs.google.com/document/d/1nw7JX-1kVaHiK8wBteM96xAWE3dh5wRUeC691bGuFjk/edit?usp=sharing).

Join the conversation on Twitter [@ProjectMONAI](https://twitter.com/ProjectMONAI) or join our [Slack channel](https://forms.gle/QTxJq3hFictp31UM9).

Ask and answer questions over on [MONAI Deploy Informatics Gateway's GitHub Discussions tab](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/discussions).

## License

Copyright (c) MONAI Consortium. All rights reserved.
Licensed under the [Apache-2.0](LICENSE) license.

This software uses the Microsoft .NET 6.0 library, and the use of this software is subject to the [Microsoft software license terms](https://dotnet.microsoft.com/en-us/dotnet_library_license.htm).

By downloading this software, you agree to the license terms & all licenses listed on the [third-party licenses](docs/compliance/third-party-licenses.md) page.

## Links

- Website: <https://monai.io>
- API documentation: <https://docs.monai.io/projects/monai-deploy-informatics-gateway>
- Code: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway>
- Project tracker: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/projects>
- Issue tracker: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/issues>
- Wiki: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/wiki>
- Test status: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions>

