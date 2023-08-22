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

# Configuration

## Informatics Gateway Configuration File

The configuration file (`appsettings.json`) controls the behaviors and parameters of the internal services. The file is stored next to the main application binary and provides a subset of the default configuration options by default. Please refer to the [Monai.Deploy.InformaticsGateway.Configuration](xref:Monai.Deploy.InformaticsGateway.Configuration.InformaticsGatewayConfiguration) namespace for complete reference.

### Configuration Sections

`appsettings.json` contains the following top-level sections:

```json
{
    "ConnectionStrings": "connection string to the database",
    "InformaticsGateway": "configuration options for the Informatics Gateway & its internal services",
    "Logging": "logging configuration options",
    "Kestrel": "web server configuration options. See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-5.0",
    "AllowedHosts": "host filtering option.  See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/host-filtering?view=aspnetcore-5.0",
    "Cli": "configurations used by the CLI"
}
```

---

The `InformaticsGateway` configuration section contains the following sub-sections:

| Section   | Description                                                                        | Reference                                                                                                   |
| --------- | ---------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| dicom     | DICOM DIMSE service configuration options                                          | [DicomConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.DicomConfiguration)                 |
| dicomWeb  | DICOMweb service configuration options                                             | [DicomWebConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.DicomWebConfiguration)           |
| export    | Export service configuration options                                               | [DataExportConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.DataExportConfiguration)       |
| fhir      | FHIR service configuration options                                                 | [FhirConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.FhirConfiguration)                   |
| hl7       | HL7 listener configuration options                                                 | [Hl7Configuration](xref:Monai.Deploy.InformaticsGateway.Configuration.Hl7Configuration)                     |
| storage   | Storage configuration options, including storage service and disk usage monitoring | [StorageConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.StorageConfiguration)             |
| messaging | Message broker configuration options                                               | [MessageBrokerConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.MessageBrokerConfiguration) |
| plug-ins  | Configuration options for plug-ins                                                 | [PlugInConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.PlugInConfiguration)               |
| Cli       | The configuration used by the CLI                                                  | -                                                                                                           |

---

### Default Configuration

```json
{
    "ConnectionStrings": {
        "InformaticsGatewayDatabase": "Data Source=/database/mig.db"
    },
    "InformaticsGateway": {
        "dicom": {
            "scp": {
                "port": 104,
                "logDimseDatasets": false,
                "rejectUnknownSources": true
            },
            "scu": {
                "aeTitle": "MONAISCU",
                "logDimseDatasets": false,
                "logDataPDUs": false
            }
        },
        "messaging": {
            "publisherServiceAssemblyName": "Monai.Deploy.Messaging.RabbitMQ.RabbitMQMessagePublisherService, Monai.Deploy.Messaging.RabbitMQ",
            "publisherSettings": {
                "endpoint": "localhost",
                "username": "username",
                "password": "password",
                "virtualHost": "monaideploy",
                "exchange": "monaideploy"
            },
            "subscriberServiceAssemblyName": "Monai.Deploy.Messaging.RabbitMQ.RabbitMQMessageSubscriberService, Monai.Deploy.Messaging.RabbitMQ",
            "subscriberSettings": {
                "endpoint": "localhost",
                "username": "username",
                "password": "password",
                "virtualHost": "monaideploy",
                "exchange": "monaideploy",
                "exportRequestQueue": "export_tasks",
                "deadLetterExchange": "monaideploy-dead-letter",
                "deliveryLimit": 3,
                "requeueDelay": 30
            }
        },
        "storage": {
            "localTemporaryStoragePath": "/payloads",
            "remoteTemporaryStoragePath": "/incoming",
            "bucketName": "monaideploy",
            "storageRootPath": "/payloads",
            "temporaryBucketName": "monaideploy",
            "serviceAssemblyName": "Monai.Deploy.Storage.MinIO.MinIoStorageService, Monai.Deploy.Storage.MinIO",
            "watermarkPercent": 75,
            "reserveSpaceGB": 5,
            "settings": {
                "endpoint": "localhost:9000",
                "accessKey": "admin",
                "accessToken": "password",
                "securedConnection": false,
                "region": "local",
                "executableLocation": "/bin/mc",
                "serviceName": "MinIO"
            }
        },
        "hl7": {
            "port": 2575,
            "maximumNumberOfConnections": 10,
            "clientTimeout": 60000,
            "sendAck": true
        },
      "dicomWeb": {
        "plugins": []
      },
    },
    "Kestrel": {
        "EndPoints": {
            "Http": {
                "Url": "http://+:5000"
            }
        }
    },
    "AllowedHosts": "*",
    "Cli": {
        "Runner": "Docker",
        "HostDataStorageMount": "~/.mig/data",
        "HostPlugInsStorageMount": "~/.mig/plug-ins",
        "HostDatabaseStorageMount": "~/.mig/database",
        "HostLogsStorageMount": "~/.mig/logs",
        "InformaticsGatewayServerEndpoint": "http://localhost:5000",
        "DockerImagePrefix": "ghcr.io/project-monai/monai-deploy-informatics-gateway"
    }
}
```

### Configuration Validation

Informatics Gateway validates all configuration options at startup. Any provided values that are invalid or missing may cause the service to crash.

> [!Note]
> If the Informatics Gateway is running with Kubernetes/Helm and is reporting the `CrashLoopBack` error, it may be indicating a startup error due to misconfiguration, simply run `kubectl logs <name-of-dicom-adapter-pod>` to review the validation errors.

### Logging

Informatics Gateway, by default, is configured to writes all logs to the console as well as text files. The behaviors may be changed in the `nlog.config` file.

Logs files are stored in the `logs/` directory where the Informatics Gateway executable is stored. To change the location, modify the `logDir` variable defined in the `nlog.config` file.

Informaitcs Gateway also supports shipping logs to [LogStash, ELK](https://www.elastic.co/elastic-stack/) using the [Network target](https://github.com/NLog/NLog/wiki/Network-target) provided by [NLog](https://nlog-project.org/). To enable this feature, simply set the environment variable `LOGSTASH_URL` to the TCP endpoint of LogStash. E.g. `LOGSTASH_URL=tcp://my-logstash-ip:5000`.

To use other logging services, refer to [NLog Config](https://nlog-project.org/config/).

> [!Note]
> If the Informatics Gateway is running inside a Docker container, additional configuration may be required to limit the size to prevent logs from filling up storage space. Refer to the [Docker documentation](https://docs.docker.com/config/containers/logging/configure/) for additional information.
