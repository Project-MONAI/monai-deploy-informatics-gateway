<!--
  ~ Copyright 2021-2023 MONAI Consortium
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


# Changelog

## 0.4.0

[GitHub Milestone 0.4.0](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/5)

- gh-435 Fix CLI to read log dir path from NLog config file.
- gh-425 New Virtual Application Entity support for DICOMWeb STOW-RS APIs to enable dynamic endpoints
- gh-421 Integrate updated Workflow Request data structure to support multiple sources.
- New data [plug-ins](./plug-ins/overview.md) feature to manipulate incoming outgoing data.


## 0.3.21

[GitHub Milestone 0.3.21](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/26)

- Remove the need to double-copy files to storage service.

## 0.3.20

[GitHub Milestone 0.3.20](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/25)

- gh-396 Spawn new thread for processing echo requests.

## 0.3.19

[GitHub Milestone 0.3.19](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/24)

- gh-392 Fix GET `/config/aetitle` URL.

## 0.3.18

[GitHub Milestone 0.3.18](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/23)

- gh-390 New API to retrieve registered source AETs

## 0.3.11

[GitHub Milestone 0.3.17](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/22)

- gh-385 Resets ActionBlock if faulted or cancelled in the Payload assembler pipeline.


## 0.3.16

[GitHub Milestone 0.3.16](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/21)

- gh-347 Set time limit when calling Storage List/Verify APIs.

## 0.3.15

[GitHub Milestone 0.3.15](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/20)

- New APIs for managing SCP AE Titles
  - `PUT /config/ae`: [Update SCP AE TItle](./api/rest/config.md#put-configae)

## 0.3.14

[GitHub Milestone 0.3.14](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/19)

- Fixes RabbitMQ startup issues.

## 0.3.13

[GitHub Milestone 0.3.13](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/18)

- Fixes an issue where failure uploads caused payload to stuck in the queue and stops processing any incoming data.

## 0.3.12

[GitHub Milestone 0.3.12](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/17)

- Fixes exception handling for unavailable previously created dead-letter queues

## 0.3.11

[GitHub Milestone 0.3.11](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/16)

- Adds exception handling for unavailable previously created dead-letter queues

## 0.3.10

[GitHub Milestone 0.3.10](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/15)

- Fixes payload assembler not respecting user configured timeout window

## 0.3.8

[GitHub Milestone 0.3.8](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/14)

- Clears payloads that are created by the same instance of MIG at startup.
- Fixes bad Mongodb configuration resulted in GUIDs not being (de)serialized correctly.


## 0.3.7

[GitHub Milestone 0.3.7](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/13)

- Fixes database health checks not using the configured database.

## 0.3.6

[GitHub Milestone 0.3.6](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/12)

- Adds support for basic auth with Monai.Deploy.Security 0.1.3.
- Updates APIs to store the username of the person who initiated the calls.
- Fixes database health checks not using the configured database.

## 0.3.5

[GitHub Milestone 0.3.5](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/11)

- Integrates Monai.Deploy.Security to enable OpenID Connect for API authentication.
- Records DICOM association information in the database.

## 0.3.4

[GitHub Milestone 0.3.4](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/10)

- Adds support for MongoDB in addition to SQLite.
- Improves validation for AE Title, IP address, and host/domain names.

## 0.3.3

[GitHub Milestone 0.3.3](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/9)

- Ability to create storage buckets on startup
- Includes logging scope values for LogStash


## 0.3.2

[GitHub Milestone 0.3.2](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/8)

- The default logging framework has changed to [NLog](https://nlog-project.org/) to enable logging to ELK and other logging services.
- New APIs for managing DICOM sources & DICOM destinations
  - `PUT /config/source`: [Update DICOM source](./api/rest/config.md#put-configsource)
  - `PUT /config/destination`: [Update DICOM destination](./api/rest/config.md#put-configdestination)
  - `GET /config/destination/cecho/{name}`: [C-ECHO DICOM destination](./api/rest/config.md#get-configdestinationcechoname)
- Updated the following APIs to return 409 if entity already existed:
  - `POST /config/ae`: [Create MONAI SCP AE](./api/rest/config.md#post-configae)
  - `POST /config/source`: [Create DICOM source](./api/rest/config.md#post-configsource)
  - `POST /config/destination`: [Create DICOM destination](./api/rest/config.md#post-configdestination)
  - Bug fixes & performance improvements.

## 0.3.1

[GitHub Milestone 0.3.1](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/7)

- New [C-ECHO API](api/rest/config.md) to perform a DICOM C-ECHO to a configured DICOM destination.
- The SCU AE Title is now uppercase MONAISCU.
- Updates fo-dicom to 5.0.3
- Defaults temporary storage to use disk with ability to switch to memory.

## 0.3.0

[GitHub Milestone 0.3.0](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/3)

- Adds a basic [FHIR service](api/rest/fhir.md) to accept any versions of FHIR.
- Updates [Health Check API](api/rest/health.md) to replace `/health/live` and `/health/ready` APIs with `/health` API.

## 0.2.0

[GitHub Milestone 0.2.0](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/6)

- Adds DICOMWeb STOW-RS service to enable triggering of workflow requests via DICOMWeb standard.
- Adds HL7 (MLLP) service to enable triggering of workflow requests using HL7 messages.
- Breaking changes with how Informatics Gateway handles incoming data and uploading to the storage service.
  All incoming data are now immediately queued for upload to the storage service instead of saving to the local disk.
  Therefore, if Informatics Gateway restarts or crashes during upload, any queued or incomplete uploads are lost.
  In addition, during the Informatics Gateway startup, the payload assembly service removes any payloads containing
  any pending files. Files that were successfully uploaded to the temporary location (`storage>temporary`) in the
  bucket (`storage>temporaryBucketName`) are then moved to the payload bucket (`storage>bucketName`) before sending a workflow request.
- Breaking changes in the storage configuration to allow dynamic key-value pairs.
- Breaking changes to enable dynamic loading of the storage & the messaging libraries.

## 0.1.1

[GitHub Milestone 0.1.1](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/6)

- User guide updates & minor bug fixes.

The MONAI Deploy Informatics Gateway upgrades the existing NVIDIA Clara Deploy DICOM Adapter to provide additional features and integrate with the MONAI Deploy platform.

- DICOM SCP (C-ECHO & C-STORE), SCU (C-STORE) support
- ACR API with ability to retrieve data via DICOMweb & FHIR
- Integrates with MinIO as the default storage service for storing received/retrieve data and for sharing among other subsystems in MONAI Deploy platform.
- Integrates with RabbitMQ as the default messaging broker for exchanging requests among other subsystems.


For a complete list of supported features, pleas refer to the [User Guide](./index.md).
