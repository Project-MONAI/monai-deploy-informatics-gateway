<!--
SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
SPDX-License-Identifier: Apache License 2.0
-->


# Command-line Interface (CLI)

The command-line interface (CLI) allows users to configure settings and
control the Informatics Gateway.

## Available Commands

Use the `mig-cli` command to see all available commands:

```bash
> mig-cli

mig-cli
  MONAI Deploy Informatics Gateway CLI

Usage:
  mig-cli [options] [command]

Options:
  -v, --verbose   Show verbose output [default: False]
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  config                  Configure the CLI endpoint
  start                   Start the MONAI Deploy Informatics Gateway service
  stop                    Stop the MONAI Deploy Informatics Gateway service
  restart                 Restart the MONAI Deploy Informatics Gateway service
  aet, aetitle            Configure SCP Application Entities
  source, src             Configure DICOM sources
  dest, destination, dst  Configure DICOM destinations
  status                  MONAI Deploy Informatics Gateway service status
```

## Help & Logging

The verbose switch `-v` and help switch `-h` are available for all commands.

The following command gets help for the `aet add` command:

```bash
> mig-cli aet add -h

add
  Add a new SCP Application Entity

Usage:
  mig-cli [options] aet add

Options:
  -n, --name <name>                   Name of the SCP Application Entity
  -a, --aetitle <aetitle> (REQUIRED)  AE Title of the SCP
  -g, --grouping <grouping>           DICOM tag used to group instances [default: 0020,000D]
  -t, --timeout <timeout>             Timeout, in seconds, to wait for instances [default: 5]
  -w, --workflows <workflows>         A space separated list of workflow names or IDs to be associated with the SCP AE Title [default: ]
  -i, --ignored-sops <ignored-sops>   A space separated list of SOP Class UIDs to be ignoredS [default: ]
  -v, --verbose                       Show verbose output [default: False]
  -?, -h, --help                      Show help and usage information
```


## Controlling Informatics Gateway

Use the following commands to start, stop, or restart the Informatics Gateway:

```bash
mig-cli start
mig-cli stop
mig-cli restart
```

## System Health

The `mig-cli status` command displays the status of all running services inside the Informatics
Gateway, as well as the number of active associations.

```bash
> mig-cli status

info: Number of active DIMSE connections: 8
info: Service Status:
info:           space Reclaimer Service: Running
info:           dicom SCP Service: Running
info:           dicoMweb Export Service: Running
info:           dicom Export Service: Running
info:           data Retrieval Service: Running
info:           payload Notification Service: Running
```

## Configure AE Titles

The CLI provides commands to configure the listening, source, and destination AE Titles. Each
command allows you to add (`add`), delete (`rm`), and list (`list`) configured AE Titles. Use the
`-h` switch for additional options.

### Listening AE Titles

```bash
> mig-cli aet

Description:
  Configure SCP Application Entities

Usage:
  mig-cli aet [command] [options]

Options:
  -?, -h, --help  Show help and usage information
  -v, --verbose   Show verbose output [default: False]

Commands:
  add       Add a new SCP Application Entity
  del, rm   Remove a SCP Application Entity
  list, ls  List all SCP Application Entities
```

### Source AE Titles

```bash
> mig-cli src

Description:
  Configure DICOM sources

Usage:
  mig-cli src [command] [options]

Options:
  -?, -h, --help  Show help and usage information
  -v, --verbose   Show verbose output [default: False]

Commands:
  add       Add a new DICOM source
  del, rm   Remove a DICOM source
  list, ls  List all DICOM sources

```

### Destination/Export AE Titles

```bash
>  mig-cli dst

Description:
  Configure DICOM destinations

Usage:
  mig-cli dst [command] [options]

Options:
  -?, -h, --help  Show help and usage information
  -v, --verbose   Show verbose output [default: False]

Commands:
  add       Add a new DICOM destination
  del, rm   Remove a DICOM destination
  list, ls  List all DICOM destinations
```
