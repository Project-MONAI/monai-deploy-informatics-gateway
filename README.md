<p align="center">
  <img src="https://raw.githubusercontent.com/Project-MONAI/MONAI/dev/docs/images/MONAI-logo-color.png" width="50%" alt='project-monai'>
</p>

💡 If you want to know more about MONAI Deploy WG vision, overall structure, and guidelines, please read [MONAI Deploy](https://github.com/Project-MONAI/monai-deploy) main repo first.


# MONAI Deploy Informatics Gateway

[![License](https://img.shields.io/badge/license-Apache%202.0-green.svg)](LICENSE)
[![codecov](https://codecov.io/gh/Project-MONAI/monai-deploy-informatics-gateway/branch/main/graph/badge.svg?token=34S8VI0XGD)](https://codecov.io/gh/Project-MONAI/monai-deploy-informatics-gateway)
[![Default](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions/workflows/build.yml)


MONAI Deploy Informatics Gateway (MIG), handles the first and last step in a clinical data pipeline, the Input and Output (I/O). 

It uses standard protocols like DICOM and FHIR, stores studies and resources from the medical record system as payloads, and notifies the rest of the MONAI Deploy system, specifically the [MONAI Deploy Workflow Manager](https://github.com/Project-MONAI/monai-deploy-workflow-manager), that data is ready to be processed. 

After inference is done, it receives the results and send them to the proper consumers, usually PACS or viewers for visualization, VNAs for storage, and EHRs (Electronic Healthcare Records).  

## Services

For a list of services hosted and supported by the Informatics Gateway, please refer to the [User Guide](./docs/index.md) section.

## Installation

Please refer to the latest [user guide](./docs/setup/setup.md) for installation instructions.

## Getting started

### Runtime Requirements

* Docker 20.10.7 or later

### Development Requirements

* [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)

During development, change any settings inside the `appsettings.Development.json` file.
First, export the following environment variable before executing `dotnet run`:

#### Linux 

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

## Links

- Website: <https://monai.io>
- API documentation: <https://docs.monai.io/projects/monai-deploy-informatics-gateway>
- Code: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway>
- Project tracker: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/projects>
- Issue tracker: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/issues>
- Wiki: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/wiki>
- Test status: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions>

