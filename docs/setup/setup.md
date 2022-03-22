# Setup

This section outlines the steps to download and install Informatics Gateway using the CLI and Docker image.

## Runtime Requirements

* Docker 20.10.12 or higher

For development requirements, please refer to [README.md](https://github.com/Project-MONAI/monai-deploy-informatics-gateway).

## Installation

### Informatics Gateway CLI

Download & install Informatics Gateway CLI from the [Releases](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/releases) section of
the repository and install it.

#### On Linux

```bash
# Download the CLI
curl -LO https://github.com/Project-MONAI/monai-deploy-informatics-gateway/releases/download/0.1.0/mig-cli-0.1.0-linux-x64.zip
# Calculate the SHA256 checksum and verify the output with the checksum on the Releases page.
sha256sum mig-cli-0.1.0-linux-x64.zip
# Unzip the CLI
unzip mig-cli-0.1.0-linux-x64.zip
# Install it in bin
sudo mv mig-cli /usr/local/bin
```

#### On Windows

```powershell
# Download the CLI
curl -LO https://github.com/Project-MONAI/monai-deploy-informatics-gateway/releases/download/0.1.0/mig-cli-0.1.0-win-x64.zip
# Calculate the SHA256 checksum and verify the output with the checksum on the Releases page.
Get-FileHash mig-cli-0.1.0-win-x64.zip
# Unzip the CLI
Expand-Archive -Path mig-cli-0.1.0-win-x64.zip
```

### Informatics Gateway Docker Image

Navigate to the [monai-deploy-informatics-gateway package](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/pkgs/container/monai-deploy-informatics-gateway)
page and locate the version to download.

```bash
# for the latest build
docker pull ghcr.io/project-monai/monai-deploy-informatics-gateway:latest
# or for a versioned build
docker pull ghcr.io/project-monai/monai-deploy-informatics-gateway:0.1.0
```

## Configure Informatics Gateway

Use the following command to initialize Informatics Gateway & default configuration:

```bash
mig-cli config init
mig-cli config endpoint http://localhost:5000
```


```powershell
mig-cli.exe config init
mig-cli.exe config endpoint http://localhost:5000
```

This first command extract the default `appsettings.json` file into the home directory:

* Linux: `~/.mig/appsettings.json`
* Windows: `C:\Users\[username]\.mig\appsettings.json`

This file is mapped into the Docker container and used by the Informatics Gateway when `mig-cli` is used to launch the application.

For a complete reference of the `appsettings.jon`, please refer to [Configuration Schema](schema.md).

The second command tells `mig-cli` where the endpoint is for the Informatics Gateway RESTful API.

> [!Note]
> To see available commands simply execute `mig-cli` or `mig-cli.exe`.

## Storage Consideration & Configuration

Informatics Gateway operates on two storage locations. The first is for temporarily storing of the incoming data for data grouping. The second is where Informatics Gateway uploads grouped datasets to final storage shared by other MONAI Deploy sub-systems.

### Temporary Storage of Incoming Dataset

The temporary storage location, by default, is set to `/payloads` in the `appsettings.json` file.

To change the temporary storage location, please locate `./InformaticsGateway/storage/temporary` property in the `appsettings.json` file.

> [!Note]
> Calculate the required temporary storage based on the number of studies and the size of each study.
> Please also consider the AE Title timeout if the AE Title needs to wait a long time before assembling & uploading
> the payload for final storage.


> [!Note]
> Before running Informatics Gateway, adjust the values of `watermarkPercent` and `reserveSpaceGB` based on
> the expected number of studies and size of each study. The suggested value for `reserveSpaceGB` is 2x to 3x the
> size of a single study multiplied by the number of configured AE Titles.

### Shared Storage
Informatics Gateway includes MinIO as the default storage service provider. To integrate with another storage service provider, please refer to the [Data Storage](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/blob/main/guidelines/srs.md#data-storage) section of the SRS.

Please download and install MinIO by following the [quickstart guide](https://docs.min.io/docs/minio-quickstart-guide.html). Once MinIO is installed and configured, modify the storage configuration to enable communication between Informatics Gateway & MinIO.

Locate the storage section of the configuration in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "InformaticsGatewayDatabase": "Data Source=migdev.db"
  },
  "InformaticsGateway": {
    "dicom": { ... },
    "storage": {
      "storageServiceCredentials": {
        "endpoint": "192.168.1.1:9000", # IP & port to MinIO instance
        "accessKey": "admin", # Access key or username
        "accessToken": "password" # Access token or password
      },
      "storageService": "Monai.Deploy.InformaticsGateway.Storage.MinIoStorageService, Monai.Deploy.InformaticsGateway.Storage.MinIo", # Fully qualified type name of the storage service
      "securedConnection": false, # Indicates if a secured connection is required to access MinIO
      "storageServiceBucketName": "igbucket" # The name of the bucket where data is uploaded to
    },
    ...
  }
}
```

### Message broker

Informatics Gateway communicates with other MONAI Deploy components through a message broker.  The default messaging service
included is provided by [RabbitMQ](https://www.rabbitmq.com/). To integrate with another storage service provider, please refer
to the [Data Storage](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/blob/main/guidelines/srs.md#message-broker) section of the SRS.

To use the default messaging service, please download and install RabbitMQ by following the
[Get Started](https://www.rabbitmq.com/#getstarted) page.

Before launching Informatics Gateway, update `appsettings.json` to configure the publisher and subscriber settings.
IG publishes all messages to an *exchange* under the specified *virtual host*. Therefore, please confirm the values before starting
Informatics Gateway.

```json
{
  "InformaticsGateway": {
    "messaging": {
      "publisherSettings": {
        "endpoint": "localhost",
        "username": "username",
        "password": "password",
        "virtualHost": "monaideploy",
        "exchange": "monaideploy"
      },
      "subscriberSettings": {
        "endpoint": "localhost",
        "username": "username",
        "password": "password",
        "virtualHost": "monaideploy",
        "exchange": "monaideploy",
        "exportRequestQueue": "export_tasks"
      }
    },
  }
}
```

## Start/Stop Informatics Gateway

To start or stop Informatics Gateway, update the value of `DockerImagePrefix` in `appsettings.json` with the repository name of the Docker image:

```json
{
    ...,
    "Cli": {
        "DockerImagePrefix": "ghcr.io/project-monai/monai-deploy-informatics-gateway"
    }
}
```


Lastly, use one of the following commands start or stop Informatics Gateway:

```bash
mig-cli start
mig-cli stop
```


## Enable Incoming Associations

The next step is to configure Informatics Gateway enable receiving of DICOM instances from external DICOM devices.

1. Configure a listening AE Title to receive instances:

```bash
mig-cli aet add -a BrainAET -grouping 0020,000E, -t 30
```

The command creates a new listening AE Title with AE Title `BrainAET`.  The listening AE Title
will be grouping instances by the Series Instance UID (0020,000E) with a timeout value of 30 seconds.

> [!Note]
> `-grouping` is optional with default of 0020,000D.
> `-t` is optional with default of 5 seconds.
> For complete reference, please refer to the [Config API](../api/rest/config.md).

2. Enable receiving DICOM instances from an external DICOM devices:

```bash
mig-cli src add -n PACS-LA -a PACSLA001 --h 20.10.30.55
```

This command above tells Informatics Gateway to accept instances from AE Title `PACSLA001` at IP `20.10.30.55` and port `104`.

> [!Note]
> By default, Informatics Gateway blocks all unknown sources.
> To allow all unknown sources, set `dicom>scp>rejectUnknownSources` to `false` in `appsettings.json`.

> [!WARNING]
> Informatics Gateway validates both the source IP address and AE Title when `rejectUnknownSources` is set to `true`.
> When running Informatics Gateway in a container and data is coming from the localhost, the IP address may not be the host's IP address. In such a case, open the log file and locate the association that failed; the log should indicate the correct IP address under `Remote host`.

## Export Processed Results

If exporting via DIMSE is required, add a DICOM destination:

```bash
mig-cli dst add -a WORKSTATION1 -h 100.200.10.20 -p 104
```

The command adds a DICOM export destination with AE Title `WORKSTATION1` at IP `100.200.10.20` and port `104`.
