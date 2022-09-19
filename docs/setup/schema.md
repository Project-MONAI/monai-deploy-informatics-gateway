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
| hl7       | HL7 listener configuration options                                                 | [Hl7Configuration](xref:Monai.Deploy.InformaticsGateway.Configuration.Hl7Configuration)                          |
| storage   | Storage configuration options, including storage service and disk usage monitoring | [StorageConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.StorageConfiguration)             |
| messaging | Message broker configuration options                                               | [MessageBrokerConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.MessageBrokerConfiguration) |
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
      "bufferRootPath": "./temp",
      "tempStorageRootPath": "/incoming",
      "bucketName": "monaideploy",
      "storageRootPath": "/payloads",
      "temporaryBucketName": "monaideploy",
      "serviceAssemblyName": "Monai.Deploy.Storage.MinIO.MinIoStorageService, Monai.Deploy.Storage.MinIO",
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
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Dicom": "Information",
      "System": "Warning",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Warning",
      "Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker": "Error",
      "Monai": "Information"
    },
    "Console": {
      "FormatterName": "Systemd",
      "FormatterOptions": {
        "ColorBehavior": "Disabled",
        "IncludeScopes": true,
        "SingleLine": false,
        "TimestampFormat": " HH:mm:ss ",
        "UseUtcTimestamp": true
      }
    },
    "File": {
      "BasePath": "logs",
      "FileEncodingName": "utf-8",
      "DateFormat": "yyyyMMdd",
      "CounterFormat": "000",
      "MaxFileSize": 10485760,
      "IncludeScopes": true,
      "MaxQueueSize": 100,
      "TextBuilderType": "Monai.Deploy.InformaticsGateway.Logging.FileLoggingTextFormatter, Monai.Deploy.InformaticsGateway",
      "Files": [
        {
          "Path": "MIG-<date>-<counter>.log"
        }
      ]
    }
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

Informatics Gateway, by default, is configured to writes all logs to the console as well as text files. The behaviors may be changed in the `Logging` section of the `appsettings.json` file.

> [!Note]
> If the Informatics Gateway is running inside a Docker container, additional configuration may be required to limit the size to prevent filling up storage space. Refer to the [Docker documentation](https://docs.docker.com/config/containers/logging/configure/) for additional information.

#### Log Levels

By default, the Monai namespace logs all Information level logs. However, the log level may be adjusted on a per-module basis. For example, given the following log entries:

```
 14:26:13 info: Monai.Deploy.InformaticsGateway.Services.Connectors.WorkloadManagerNotificationService[0]
      MONAI Workload Manager Notification Hosted Service is running.
 14:26:13 info: Monai.Deploy.InformaticsGateway.Services.Storage.SpaceReclaimerService[0]
      Disk Space Reclaimer Hosted Service is running.
```

If additional information is required to debug the **WorkloadManagerNotificationService** module or to turn down the noise, add a new entry under the LogLevel section of the configuration file to adjust it:

```
 "Logging": {
    "LogLevel": {
      "Monai": "Information",
      "Monai.Deploy.InformaticsGateway.Services.Connectors.WorkloadManagerNotificationService": "Debug",
      ...
```

The following log levels may be used:

- Trace
- Debug
- Information
- Warning
- Error
- Critical
- None

Additional information may be found on `docs.microsoft.com`:

- [LogLevel Enum](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel)
- [Logging in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)
