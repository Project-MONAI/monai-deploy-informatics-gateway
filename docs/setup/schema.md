# Configuration

## Informatics Gateway Configuration File

The configuration file is a JSON formatted file used to control the behaviors and parameters of the internal services. The file, `appsettings.json`, is stored next to the main application binary and provides a subset of the default configuration options by default. Please refer to the [Monai.Deploy.InformaticsGateway.Configuration](xref:Monai.Deploy.InformaticsGateway.Configuration.InformaticsGatewayConfiguration) namespace for complete reference.


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

| Section  | Description                               | Reference                                                                                             |
| -------- | ----------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| dicom    | DICOM DIMSE service configuration options | [DicomConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.DicomConfiguration)           |
| dicomWeb | DICOMweb service configuration options    | [DicomWebConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.DicomWebConfiguration)     |
| export   | Export service configuration options      | [DataExportConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.DataExportConfiguration) |
| fhir     | FHIR service configuration options        | [FhirConfiguration](xref:Monai.Deploy.InformaticsGateway.Configuration.FhirConfiguration)             |

---

### Default Configuration

```json
{
  "ConnectionStrings": {
    "InformaticsGatewayDatabase": "Data Source=mig.db"
  },
  "InformaticsGateway": {
    "dicom": {
      "scp": {
        "port": 1104,
        "logDimseDatasets": false,
        "rejectUnknownSources": true
      },
      "scu": {
        "aeTitle": "MonaiSCU",
        "logDimseDatasets": false,
        "logDataPDUs": false
      }
    },
    "storage": {
      "temporary": "/payloads"
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
      "BasePath": "Logs",
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
        "Url": "http://localhost:5000"
      }
    }
  },
  "AllowedHosts": "*"
}
```

### Configuration Validation

Informatics Gateway validates all configuration options at startup. Any provided values that are invalid or missing may cause the service to crash.

> [!Note]
> If you are running Informatics Gateway inside Kubernetes/Helm, you may see the `CrashLoopBack` error. To review the validation errors, simply run `kubectl logs <name-of-dicom-adapter-pod>`.

### Logging

Informatics Gateway, by default, is configured to writes all logs to the console as well as text files. The behaviors may be changed in the `Logging` section of the `appsettings.json` file.

> [!Note]
> If the Informatics Gateway is running inside a Docker container, additional configuration may be required to limit the size to prevent filling up storage space. Please refer to [Docker](https://docs.docker.com/config/containers/logging/configure/) for additional information.

#### Log Levels

Log level may be adjusted on a per-module basis. For example, given the following log entries:

```
 14:26:13 info: Monai.Deploy.InformaticsGateway.Services.Connectors.WorkloadManagerNotificationService[0]
      MONAI Workload Manager Notification Hosted Service is running.
 14:26:13 info: Monai.Deploy.InformaticsGateway.Services.Storage.SpaceReclaimerService[0]
      Disk Space Reclaimer Hosted Service is running.
```

By default, the `Monai` namespace is to log all `Information` level logs. However, if additional information is required to debug the **WorkloadManagerNotificationService** module or to turn down the noise, add a new entry under the `LogLevel` section of the configuration file to adjust it:

```
 "Logging": {
    "LogLevel": {
      "Monai": "Information",
      "Monai.Deploy.InformaticsGateway.Services.Connectors.WorkloadManagerNotificationService": "Debug",
      ...
```

The following log level may be used:

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
