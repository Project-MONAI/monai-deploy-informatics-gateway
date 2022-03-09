<!--
SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
SPDX-License-Identifier: Apache License 2.0
-->

# Changelog

## 0.1.1

- User guide updates & minor bug fixes.

## 0.1.0

The MONAI Deploy Informatics Gateway upgrades the existing NVIDIA Clara Deploy DICOM Adapter to provide additional features and integrate with the MONAI Deploy platform.

- DICOM SCP (C-ECHO & C-STORE), SCU (C-STORE) support
- ACR API with ability to retrieve data via DICOMweb & FHIR
- Integrates with MinIO as the default storage service for storing received/retrieve data and for sharing among other subsystems in MONAI Deploy platform.
- Integrates with RabbitMQ as the default messaging broker for exchanging requests among other subsystems.


For a complete list of supported features, pleas refer to the [User Guide](./index.md).
