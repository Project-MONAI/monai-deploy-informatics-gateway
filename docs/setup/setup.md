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
# Setup

This section outlines the steps to download and install the Informatics Gateway using the CLI and Docker image.

## Runtime Requirements

* Docker 20.10.12 or higher
* [Database service](#database-configuration)
* [Message Broker service](#message-broker)
* [Storage service](#storage-service)

For development requirements, refer to the [Informatics Gateway README.md](https://github.com/Project-MONAI/monai-deploy-informatics-gateway).

> [!Note]
> Use [MONAI Deploy Express](https://github.com/Project-MONAI/monai-deploy/tree/main/deploy/monai-deploy-express) to quickly
> bring up all required services, including the Informatics Gateway.
> 
> Skip to [Configure Informatics Gateway](#configure-informatics-gateway) if you are using MONAI Deploy Express.



## Installation

### Informatics Gateway CLI

Download and install the Informatics Gateway CLI from the [Releases](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/releases) section of
the repository and install it.

> [!Note]
> We use `v0.2.0` release as an example here, always download the latest from the [Releases](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/releases) section.

#### On Linux

```bash
# Download the CLI
curl -LO https://github.com/Project-MONAI/monai-deploy-informatics-gateway/releases/download/0.2.0/mig-cli-0.2.0-linux-x64.zip
# Calculate the SHA256 checksum and verify the output with the checksum on the Releases page.
sha256sum mig-cli-0.2.0-linux-x64.zip
# Unzip the CLI
unzip mig-cli-0.2.0-linux-x64.zip
# Install it in bin
sudo mv mig-cli /usr/local/bin
```

#### On Windows

```powershell
# Download the CLI
curl -LO https://github.com/Project-MONAI/monai-deploy-informatics-gateway/releases/download/0.2.0/mig-cli-0.2.0-win-x64.zip
# Calculate the SHA256 checksum and verify the output with the checksum on the Releases page.
Get-FileHash mig-cli-0.2.0-win-x64.zip
# Unzip the CLI
Expand-Archive -Path mig-cli-0.2.0-win-x64.zip
```

### Informatics Gateway Docker Image

Navigate to the [monai-deploy-informatics-gateway package](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/pkgs/container/monai-deploy-informatics-gateway)
page and locate the version to download.

```bash
# for the latest build
docker pull ghcr.io/project-monai/monai-deploy-informatics-gateway:latest
# or for a versioned build
docker pull ghcr.io/project-monai/monai-deploy-informatics-gateway:0.2.0
```

## Configure Informatics Gateway

Use the following commands to initialize the Informatics Gateway and default configuration:

```bash
mig-cli config init
mig-cli config endpoint http://localhost:5000 #skip if running locally
```

```powershell
mig-cli.exe config init
mig-cli.exe config endpoint http://localhost:5000 #skip if running locally
```

> [!Note]
> For [MONAI Deploy Express](https://github.com/Project-MONAI/monai-deploy/tree/main/deploy/monai-deploy-express), use `http://localhost:5003`.

The first command extracts the default `appsettings.json` file into the home directory:

* Linux: `~/.mig/appsettings.json`
* Windows: `C:\Users\[username]\.mig\appsettings.json`

This file is mapped into the Docker container and used by the Informatics Gateway when `mig-cli` is used to launch the application.

For a complete reference of the `appsettings.json` file, refer to the [Configuration Schema](schema.md).

The second command passes the endpoint for the Informatics Gateway RESTful API to `mig-cli`.

> [!Note]
> To see available commands, simply execute `mig-cli` or `mig-cli.exe`.
> Refer to [CLI](./cli.md) for complete reference.


## Database Configuration

The Informatics Gateway supports the following database systems:

- SQLite (default)
- MongoDB

### SQLite (default)

SQLite is a lite weight, full-featured SQL database engine and is the default database engine used by the Informatics Gateway.
With SQLite, the Informatics Gateway works out of the box without any external database service dependencies or configuration.

The default configuration maps the database file `mig.db` in the `/database` directory.

```json
{
  "ConnectionStrings": {
    "Type": "sqlite",
    "InformaticsGatewayDatabase": "Data Source=/database/mig.db"
  }
}
```

### MongoDB

For enterprise installations, [MongoDB](https://www.mongodb.com/) is the recommended database solution. To switch from SQLite
to MongoDB, edit the `appsettings.json` file, and change the `ConnectionStrings` section to the following with the correct
username, password, IP address/hostname, and port.

```json
{
  "ConnectionStrings": {
    "Type": "mongodb",
    "InformaticsGatewayDatabase": "mongodb://username:password@IP:port",
    "DatabaseName": "InformaticsGateway"
  }
}
```

## Other Database Systems

Extending the Informatics Gateway to support other database systems can be done with a few steps.

If the database system is supported by [Microsoft Entity Framework](https://learn.microsoft.com/en-us/ef/core/providers/), then it can be added to the existing [project](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/tree/develop/src/Database/EntityFramework).

For other database systems that are not listed in the link above, simply implement the [Repository APIs](xref:Monai.Deploy.InformaticsGateway.Database.Api.Repositories), update the [Database Manager](xref:Monai.Deploy.InformaticsGateway.Database.DatabaseManager) to support the new database type and optionally, implement the [IDabaseMigrationManager](xref:Monai.Deploy.InformaticsGateway.Database.Api.IDatabaseMigrationManager).


## Authentication

Authentication is disabled by default. To enable authentication using OpenID, edit the `appsettings.json` file and set `bypassAuthentication` to `true`:

```json
{
  "MonaiDeployAuthentication": {
    "bypassAuthentication": true,
    "openId": {
      "realm": "{realm}",
      "realmKey": "{realm-secret-key}",
      "clientId": "{client-id}",
      "audiences": [ "{audiences}" ],
      "roleClaimType": "{roles}",
  ...
}
```

Refer to [Authentication Setup Using Keycloak](https://github.com/Project-MONAI/monai-deploy-workflow-manager/blob/develop/guidelines/mwm-auth.md) for additional details.


## Storage Consideration & Configuration

The Informatics Gateway operates on two storage locations. In the first location, the incoming data for data grouping is temporarily stored. In the second location, the Informatics Gateway uploads grouped datasets to final storage shared by other MONAI Deploy sub-systems.

### Temporary Storage of Incoming Dataset

By default, the temporary storage location is set to use `Disk` and stores any incoming files inside `/payloads`.  This can be modified to user a different location, such as `Memory` or a different path.

To change the temporary storage path, locate the `InformaticsGateway>storage>localTemporaryStoragePath` property in the `appsettings.json` file and modify it.

> [!Note]
> You will need to calculate the required temporary storage based on the number of studies and the size of each study.
> Also, consider changing the AE Title timeout if the AE Title needs to wait a long time before assembling and uploading
> the payload for final storage.

> [!Note]
> Before running the Informatics Gateway, adjust the values of `watermarkPercent` and `reserveSpaceGB` based on
> the expected number of studies and size of each study. The suggested value for `reserveSpaceGB` is 2x to 3x the
> size of a single study multiplied by the number of configured AE Titles.

### Storage Service

Informatics Gateway includes MinIO as the default storage service provider. To integrate with another storage service provider, please refer to the [Data Storage](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/blob/main/guidelines/srs.md#data-storage) section of the SRS.

Download and install MinIO by following the [quickstart guide](https://docs.min.io/docs/minio-quickstart-guide.html). Once MinIO is installed and configured, modify the storage configuration to enable communication between the Informatics Gateway and MinIO.

Locate the storage section of the configuration in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "InformaticsGatewayDatabase": "Data Source=migdev.db"
  },
  "InformaticsGateway": {
    "dicom": { ... },
    "storage": {
      "localTemporaryStoragePath": "/payloads", # path to store incoming data before uploading to the storage service
      "remoteTemporaryStoragePath": "/incoming", # the path on the "temporaryBucketName" where the data is uploaded to before payload assembly
      "bucketName": "monaideploy", # name of the bucket for storing payloads
      "temporaryBucketName": "monaideploy", # name of the bucket for temporarily storing incoming data before payload is assembled
      "serviceAssemblyName": "Monai.Deploy.Storage.MinIO.MinIoStorageService, Monai.Deploy.Storage.MinIO", # the fully qualified assembly name for the storage service to use
      "watermarkPercent": 75, # a percentage value that indicates when the system shall stop receiving or downloading data.  Disk space is calculated based on the path defined in "localTemporaryStoragePath"
      "reserveSpaceGB": 5, # minimum disk space required and reserved for the Informatics Gateway
      "settings": { # settings for the storage library: default to minio
        "endpoint": "localhost:9000", # MinIO server IP and port number
        "accessKey": "admin", # username/access key
        "accessToken": "password", # password/access token
        "securedConnection": false, # enable secured connection to minio?
        "region": "local", # storatge region
        "createBuckets": "monaideploy" # buckets to be created on startup if not already exists
      }
    },
    ...
  }
}
```

> [!Note]
> Update the `createBuckets` configuration if you would like to have the Informatics Gateway create the storage buckets on startup.  Otherwise, leave it blank.
> To create multiple buckets, separate each bucket name with comma.

#### Install the Storage Plug-in

As shown above, the default plug-in configured is __MinIO__ and is ready to use.

To use other storage plug-in, refer to [MONAI Deploy Storage](https://github.com/Project-MONAI/monai-deploy-storage/releases) for available plug-ins or bring your own plug-in.

To install a new storage plug-in, unzip the files to the `plug-ins` directory in your home directory:

* Linux: `~/.mig/plug-ins`
* Windows: `C:\Users\[username]\.mig\plug-ins`

> [!Note]
> If a plug-in other than MinIO is used, update the `storageService` parameter in the `appsettings.json` file.


### Message broker

The Informatics Gateway communicates with other MONAI Deploy components through a message broker. The default messaging service
included is provided by [RabbitMQ](https://www.rabbitmq.com/). To integrate with another storage service provider, refer
to the [Data Storage](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/blob/main/guidelines/srs.md#message-broker) section of the SRS.

To use the default messaging service, download and install RabbitMQ by following the
[Get Started](https://www.rabbitmq.com/#getstarted) page.

The Informatics Gateway publishes all messages to an *exchange* under the specified *virtual host*.
Before launching Informatics Gateway, update the `appsettings.json` file to configure the publisher
and subscriber settings.

```json
{
  "InformaticsGateway": {
    "messaging": {
      "publisherServiceAssemblyName":"Monai.Deploy.Messaging.RabbitMQ.RabbitMQMessagePublisherService, Monai.Deploy.Messaging.RabbitMQ",
      "publisherSettings": {
        "endpoint": "localhost",
        "username": "username",
        "password": "password",
        "virtualHost": "monaideploy",
        "exchange": "monaideploy"
      },
      "subscriberServiceAssemblyName":"Monai.Deploy.Messaging.RabbitMQ.RabbitMQMessageSubscriberService, Monai.Deploy.Messaging.RabbitMQ",
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

#### Install the Messaging Plug-in

As shown above, the default plug-in configured is __RabbitMQ__ and is ready to use.

To use other plug-in, refer to [MONAI Deploy Messaging](https://github.com/Project-MONAI/monai-deploy-messaging/releases) for available plug-ins.

To install a new messaging plug-in, unzip the files to the `plug-ins` directory in your home directory:

* Linux: `~/.mig/plug-ins`
* Windows: `C:\Users\[username]\.mig\plug-ins`

> [!Note]
> If a plug-in other than Rabbit is used, update the `publisherServiceAssemblyName` and `subscriberServiceAssemblyName` parameters in the `appsettings.json` file.


## Start/Stop Informatics Gateway

To start or stop the Informatics Gateway, update the value of the `DockerImagePrefix` parameter in
the `appsettings.json` file with the repository name of the Docker image (the default value is
shown below):

```json
{
    ...,
    "Cli": {
        "DockerImagePrefix": "ghcr.io/project-monai/monai-deploy-informatics-gateway"
    }
}
```


Lastly, use one of the following commands to start or stop the Informatics Gateway:

```bash
mig-cli start
mig-cli stop
```


## Enable Incoming Associations

The next step is to configure the Informatics Gateway to enable receiving of DICOM instances from external DICOM devices.

1. Configure a listening AE Title to receive instances:

```bash
mig-cli aet add -a BrainAET -grouping 0020,000E, -t 30
```

The command creates a new listening AE Title with AE Title `BrainAET`. The listening AE Title
will group instances by the Series Instance UID (0020,000E) with a timeout value of 30 seconds.


### Optional: Input Data Plug-ins

Each listening AE Title may be configured with zero or more plug-ins to maniulate incoming DICOM files before saving to the storage
service and dispatching a workflow request. To include input data plug-ins, first create your plug-ins by implementing the
[IInputDataPlugin](xref:Monai.Deploy.InformaticsGateway.Api.IInputDataPlugin) interface and then use `-p` argument with the fully
qualified type name with the `mig-cli aet add` command. For example, the following command adds `MyNamespace.AnonymizePlugin`
and `MyNamespace.FixSeriesData` plug-ins from the `MyNamespace.Plugins` assembly file.

```bash
mig-cli aet add -a BrainAET -grouping 0020,000E, -t 30 -p "MyNamespace.AnonymizePlugin, MyNamespace.Plugins" "MyNamespace.FixSeriesData, MyNamespace.Plugins"
```

> [!Note]
> `-grouping` is optional, with a default value of 0020,000D.
> `-t` is optional, with a default value of 5 seconds.
> For complete reference, refer to the [Config API](../api/rest/config.md).

2. Enable the receiving of DICOM instances from external DICOM devices:

```bash
mig-cli src add -n PACS-LA -a PACSLA001 --h 20.10.30.55
```

The above command tells the Informatics Gateway to accept instances from AE Title `PACSLA001` at IP `20.10.30.55` and port `104`.

> [!Note]
> By default, Informatics Gateway blocks all unknown sources.
> To allow all unknown sources, set the `dicom>scp>rejectUnknownSources` parameter to `false` in the `appsettings.json` file.

> [!WARNING]
> The Informatics Gateway validates both the source IP address and AE Title when `rejectUnknownSources` is set to `true`.
> When the Informatics Gateway is running in a container and data is coming from the localhost, the IP address may not be the same as the host IP address. In this case, open the log file and locate the association that failed; the log should indicate the correct IP address under `Remote host`.

## Export Processed Results

If exporting via DIMSE is required, add a DICOM destination:

```bash
mig-cli dst add -a WORKSTATION1 -h 100.200.10.20 -p 104
```

The command adds a DICOM export destination with AE Title `WORKSTATION1` at IP `100.200.10.20` and port `104`.

## Logging

See [schema](./schema.md#logging) page for additional information on logging.

## Data Plug-ins

You may write your own data plug-ins to manipulate incoming data before they are saved to the storage service or outgoing data right before they are exported.

To write an input data plug-in, implement the [IInputDataPlugin](xref:Monai.Deploy.InformaticsGateway.Api.IInputDataPlugin) interface and put the assmblye dll in the
plug-ins directories.  Similarly for output data plug-ins, implement the [IOutputDataPlugin](xref:Monai.Deploy.InformaticsGateway.Api.IOutputDataPlugin) interface.

Refer to [Configuration API](../api/rest/config.md) page to retrieve available [input](../api/rest/config.md#get-configaeplug-ins) and [output](../api/rest/config.md#get-configdestinationplug-ins) data plug-ins.
