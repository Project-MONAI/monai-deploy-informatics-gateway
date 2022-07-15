<!--
SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
SPDX-License-Identifier: Apache License 2.0
-->

# Changelog

## 0.2.0

[GitHub Milestone 0.2.0](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/6)

- Adds DICOMWeb STOW-RS service to enable triggering of workflow requests via DICOMWeb standard.
- Breaking changes in the storage configuration to allow dynamic key-value pairs.
- Breaking changes to enable dynamic loadig of the storage & the messaging libraries.

## 0.1.1

[GitHub Milestone 0.1.1](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestone/6)

- User guide updates & minor bug fixes.

The MONAI Deploy Informatics Gateway upgrades the existing NVIDIA Clara Deploy DICOM Adapter to provide additional features and integrate with the MONAI Deploy platform.

- DICOM SCP (C-ECHO & C-STORE), SCU (C-STORE) support
- ACR API with ability to retrieve data via DICOMweb & FHIR
- Integrates with MinIO as the default storage service for storing received/retrieve data and for sharing among other subsystems in MONAI Deploy platform.
- Integrates with RabbitMQ as the default messaging broker for exchanging requests among other subsystems.


For a complete list of supported features, pleas refer to the [User Guide](./index.md).
