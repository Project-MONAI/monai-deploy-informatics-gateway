

# MONAI Deploy Informatics Gateway

[![License](https://img.shields.io/badge/license-Apache%202.0-green.svg)](LICENSE)
[![codecov](https://codecov.io/gh/Project-MONAI/monai-deploy-informatics-gateway/branch/main/graph/badge.svg?token=34S8VI0XGD)](https://codecov.io/gh/Project-MONAI/monai-deploy-informatics-gateway)
[![Default](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions/workflows/build.yml)

## Build

### Prerequisites

* [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)


### Development Environment
During development, change any settings inside the `appsettings.Development.json` file.
First, export the following environment variable before executing `dotnet run`:

```bash
export DOTNET_ENVIRONMENT=Development
```

### Building MONAI Deploy Informatics Gateway

```bash
dotnet build
```
