# MONAI Informatics Gateway Requirements

## Overview

The MONAI Informatics Gateway (MIG) is the integration point between healthcare information systems and the MONAI Deploy SDK and MONAI App Server.  

This document defines the requirements for the MONAI Informatics Gateway.

## Goal
The goal for this proposal is to enlist, prioritize and provide clarity on the requirements for MONAI Informatics Gateway. Developers working on different software modules in MONAI Informatics Gateway SHALL use this specification as a guideline when designing and implementing software for the MONAI Informatics Gateway.

## Standard Language
This document SHALL follow the guidance of [rfc
2119](https://datatracker.ietf.org/doc/html/rfc2119) for terminology.

## Success Criteria
Users SHALL be able to send and receive data using standards defined by the healthcare industry including, but not limited to, [DICOM](https://www.dicomstandard.org/) and [FHIR](https://hl7.org/FHIR/).


## Requirements

### DICOM 

#### MIG SHALL respond to Verification Requests (C-ECHO)
A DICOM protocol similar to “ping” which allows remote to check the status of MONAI Informatics Gateway.

#### MIG SHALL respond to DICOM Store Requests (C-STORE)
MIG SHALL allow DICOM data to be received and stored via standard DICOM TCP C-STORE command.

#### MIG SHALL be able to export DICOM via C-STORE
MIG SHALL allow DICOM data to be sent via standard DICOM TCP-C-STORE command. This allows sending processed results to configured remote DICOM entities.

#### MIG SHOULD support Storage Commitment
MIG should support DICOM Storage Commitment service before deleting any associated data.

#### MIG SHOULD be able to query external DICOM devices (C-FIND)
MIG SHOULD allow users to issue C-FIND requests to query external DICOM devices, such as PACS.

#### MIG SHOULD be able to retrieve DICOM instances from external DICOM devices (C-MOVE)
MIG SHOULD allow users to issue C-MOVE requests to retrieve DICOM instances from external DICOM devices, such as PACS.

### DICOMweb

#### MIG SHALL be able to query DICOMweb services (QIDO-RS)
MIG SHALL be able to act as a DICOMweb client and search DICOM objects via QIDO-RS.

#### MIG SHALL be able to retrieve from DICOMweb services (WADO-RS)
MIG SHALL be able to act as a DICOMweb client and retrieve DICOM objects via WADO-RS.

#### MIG SHALL be able to export to DICOMweb services (STOW-RS)
MIG SHALL be able to act as a DICOMweb client and export DICOM objects via STOW-RS.

#### MIG SHOULD be able to accept DICOMweb query requests (QIDO-RS)
MIG SHOULD be able to act as a DICOMweb server and allow users to search available DICOM objects hosted by the MONAI App Server.

#### MIG SHOULD be able to allow users to retrieve DICOM objects via DICOMweb (WADO-RS)
MIG SHOULD be able to act as a DICOMweb server and allow users to download DICOM objects hosted by the MONAI App Server.

#### MIG SHOULD be able to allow users to upload DICOM objects (STOW-RS)
MIG SHOULD be able to act as a DICOMweb server and allow users to upload DICOM objects.
(Note: this requirement is separate from how the uploaded DICOM objects triggers new jobs.)


### FHIR

#### MIG SHOULD be able to retrieve FHIR resources
MIG SHALL be able to retrieve and store FHIR resources requested by the users.

### APIs

#### MIG SHALL allow users to trigger jobs via standard APIs
MIG SHALL allow users to trigger jobs via APIs.

### Logging

#### Log DIMSE dataset
MIG SHOULD log DIMSE dataset if enabled.

#### Anonymize logs
MIG SHOULD be able to hide any PHI in the logs.

#### Log Levels
MIG SHOULD allow users to adjust log levels.

### Configuration

#### Store SCP AE Title shall be configurable
MIG SHALL allow users to configure the SCP AE TItle for Clara DICOM Adapter.

#### Listening port shall be configurable
MIG SHALL allow users to configure the port for the DICOM TCP listening port.

#### Verification protocol shall be configurable
MIG SHALL handle or ignore C-ECHO requests based on user configuration. 

#### Store SCU AE Title shall be configurable
MIG SHALL allow users to configure the SCU AE TItle.

#### Allow exporting to multiple DICOM destinations
MIG SHALL be able to export inference results to one or more DICOM destinations.

#### Configure number of concurrent associations
MIG SHALL allow user to configure number of concurrent association that are allowed, per system.

#### Received data from configured DICOM sources
MIG SHALL accept DICOM objects from known DICOM sources and reject if not configured.

#### Configurable retry logic
MIG SHALL allow user to configure the maximum number of retries and delays in-between each retry when communicating with external systems/services.

