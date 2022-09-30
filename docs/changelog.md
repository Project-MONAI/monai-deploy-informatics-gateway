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


# Changelog

## 0.3.1

[GitHub Milestone 0.3.2](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/8)

- New [C-ECHO API](api/rest/config.md) to perform a DICOM C-ECHO to a configured DICOM destination.

## 0.3.1

[GitHub Milestone 0.3.1](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/7)

- The SCU AE Title is now uppercase MONAISCU.
- Update fo-dicom to 5.0.3
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
