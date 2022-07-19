<!--
  ~ Copyright 2022 MONAI Consortium
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

## 0.2.0

[GitHub Milestone 0.2.0](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/6)

- Adds DICOMWeb STOW-RS service to enable triggering of workflow requests via DICOMWeb standard.
- Breaking changes in the storage configuration to allow dynamic key-value pairs.

## 0.1.1

[GitHub Milestone 0.1.1](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/6)

- User guide updates & minor bug fixes.

The MONAI Deploy Informatics Gateway upgrades the existing NVIDIA Clara Deploy DICOM Adapter to provide additional features and integrate with the MONAI Deploy platform.

- DICOM SCP (C-ECHO & C-STORE), SCU (C-STORE) support
- ACR API with ability to retrieve data via DICOMweb & FHIR
- Integrates with MinIO as the default storage service for storing received/retrieve data and for sharing among other subsystems in MONAI Deploy platform.
- Integrates with RabbitMQ as the default messaging broker for exchanging requests among other subsystems.


For a complete list of supported features, pleas refer to the [User Guide](./index.md).
