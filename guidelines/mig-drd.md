<!--
SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
SPDX-License-Identifier: Apache License 2.0
-->


# MONAI Deploy Informatics Gateway Requirements

## Overview

The MONAI Deploy Informatics Gateway (MIG) integrates healthcare information systems and the MONAI Deploy platform by providing standard protocols defined by the healthcare industry.

## Purpose

This document defines the requirements for the MONAI Deploy Informatics Gateway.

## Goal

This proposal aims to enlist, prioritize, and clarify the requirements for MONAI Deploy Informatics Gateway. Developers working on different software modules in MONAI Deploy Informatics Gateway SHALL use this specification as a guideline when designing and implementing software for the MONAI Deploy Informatics Gateway.

## Standard Language

This document SHALL follow the guidance of [rfc2119](https://datatracker.ietf.org/doc/html/rfc2119) for terminology.

## Success Criteria

Users SHALL be able to send and receive data using standards defined by the healthcare industry including, but not limited to, [DICOM](https://www.dicomstandard.org/) and [FHIR](https://hl7.org/FHIR/).


## Attributes of a Requirement

Each requirement defined in this document must include the following attributes:

**Requirement Body**: Describes the goal and purpose behind the requirement.

**Background**: Provides necessary background to understand the context of the requirements.

**Verification Strategy**: A high-level plan on how to test this requirement at a system level.

**Target Release**: Specifies the target for the release of MONAI Deploy Workflow Manager.

---

## DICOM (DCM) Requirements

### [REQ-DCM-01] MIG SHALL respond to Verification Requests (C-ECHO) 

#### Background

C-ECHO is a DICOM protocol similar to “ping” which allows remote DICOM devices to check MONAI Deploy Informatics Gateway's status. It will enable administrators to test a connection between two DICOM endpoints after setup or when connection issues arise.

#### Verification Strategy
Use a C-ECHO SCU to verify that the C-ECHO SCP service responds.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ-DCM-02] MIG SHALL respond to DICOM Store Requests (C-STORE)

#### Background
C-STORE SCP enables receiving of DICOM instances from other DICOM devices via standard DICOM DIMSE C-STORE command.

#### Verification Strategy
Use a C-STORE SCU to send DICOM files to MIG's SCP.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ-DCM-03] MIG SHALL be able to export DICOM via C-STORE

#### Background
Applications and algorithms often produce results in DICOM formats, e.g., a Structured Report (SR), that requires to be exported back for radiologists for review. Therefore, the C-STORE SCU service enabled sending DICOM files to other DICOM devices.

#### Verification Strategy
Use a C-STORE SCP as a listener to receive DICOM files from MIG's SCU.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ-DCM-04] MIG SHOULD support Storage Commitment

#### Background
Since MONAI Deploy does not offer long-term storage of processed data (results), a storage commitment message ensures that the target server commits for safekeeping the data exported before removing it from the MONAI Deploy platform.

#### Verification Strategy
TBD

#### Target Release
TBD

### [REQ-DCM-05] MIG SHALL be able to query & retrieve DICOM instances from external DICOM devices via C-FIND & C-MOVE

#### Background
The C-FIND service combined with the C-MOVE service would enable users to trigger inference requests, e.g., using ACR-DSI API, and have MIG retrieve the DICOM studies for processing.

#### Verification Strategy
TBD

#### Target Release
TBD


### [REQ-DCM-06] MIG SHALL allow users to configure the SCP AE TItle 

#### Background

Administrators may often name the AE Title based on the geography of the device or the purpose of the device because there may be multiple instances.  This requirement allows them to change the default value according to their needs and avoid confusion.

#### Verification Strategy

Verify that only the configured AE Title responds to the association request.

#### Target Release

MONAI Deploy Informatics Gateway R1

### [REQ-DCM-07] MIG SHALL allow users to configure the port for the DICOM TCP listening port

#### Background

Many applications may often be listening on different ports on the same system, resulting in a conflict.  Therefore, allowing users to change the listening port for the SCP is a must.

#### Verification Strategy

Change the listening port of the SCP and verify that it could still receive C-ECHO requests.

#### Target Release

MONAI Deploy Informatics Gateway R1

### [REQ-DCM-08] Verification protocol shall be configurable

#### Background

Often users may want to disable response to C-ECHO requests similar to disabling responding to ICMP (ping) commands to avoid network attacks.

#### Verification Strategy

Disable C-ECHO and verify that the SCP no longer responds to the C-ECHO requests but still accepts C-STORE requests.

#### Target Release

MONAI Deploy Informatics Gateway R1

### [REQ-DCM-09] Store SCU AE Title shall be configurable

#### Background

Administrators may often name the AE Title based on the geography of the device or the purpose of the device because there may be multiple instances.  This requirement allows them to change the default value according to their needs and avoid confusion.

#### Verification Strategy
Change the SCU AE Title and verify that the export destination is responding to the updated AE Title.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ-DCM-10] MIG MUST accept DICOM data from multiple sources

#### Background

There is usually more than one DICOM device in a clinical environment, and MIG may often need to accept data from multiple DICOM devices. Therefore, MIG must allow users to configure multiple DICOM sources.

#### Verification Strategy

Configure multiple DICOM sources and make sure that all DICOM sources can send data to MIG.

#### Target Release

MONAI Deploy Informatics Gateway R1


### [REQ-DCM-11] MIG SHALL support exporting data to multiple DICOM destinations

#### Background

In some scenarios, users may want to export results to multiple DICOM destinations, and MIG must allow the user to configure multiple DICOM destinations.

#### Verification Strategy

Configure multiple DICOM destinations and make sure that MIG can reach them.

#### Target Release

MONAI Deploy Informatics Gateway R1

### [REQ-DCM-12] MIG SHALL support concurrent DICOM associations

#### Background

Often, DICOM devices, such as PACS, may initiate multiple associations concurrently to deliver a DICOM dataset.  Therefore, MIG must be able to accept concurrent associations.

#### Verification Strategy

Launch multiple store SCUs to send data to MIG and verify that data can be received.

#### Target Release

MONAI Deploy Informatics Gateway R1

### [REQ-DCM-13] MIG SHALL reject DICOM association from unknown sources

#### Background

To prevent receiving data from unknown DICOM sources, MIG must identify and validate the caller's identity, such as their AE Title, IP address, etc...

#### Verification Strategy

Send a C-ECHO request to MIG using an AE Title that is not configured on MIG and verify that the response is rejected with an appropriate reason.

#### Target Release

MONAI Deploy Informatics Gateway R1


---

## DICOMweb (DCW) Requirements

### [REQ-DCW-01] MIG SHALL be able to query DICOMweb services (QIDO-RS)

#### Background
The QIDO-RS client enables MIG to query DICOMweb servers by specifying Accession Number, Patient, and other DICOM tags.

#### Verification Strategy
Query a DICOMweb server using the client and ensure that the results match the input criteria.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ-DCW-02] MIG SHALL be able to retrieve from DICOMweb services via WADO-RS

#### Background
With WADO-RS APIs and the ACR API, the users could easily trigger an inference request by specifying the DICOM studies for an inference request without setting up DICOM DIMSE services.

#### Verification Strategy
Retrieve DICOM data from a DICOMweb server.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ-DCW-03] MIG SHALL be able to export to DICOMweb services via STOW-RS

#### Background
The STOW-RS client allows MIG to export DICOM results to external DICOMweb servers. By combining with ACR API, the users may have the inference results exported to specified DICOMweb devices.

#### Verification Strategy
Export a DICOM object to a DICOMweb server using the DICOMweb STOW client.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ-DCW-04] MIG SHALL be able to accept DICOMweb QIDO-RS (query) requests


#### Background

TBD

#### Verification Strategy

TBD

#### Target Release

TBD

### [REQ-DCW-05] MIG SHALL be able to allow users to retrieve DICOM objects via DICOMweb WADO-RS


#### Background
TBD
#### Verification Strategy
TBD
#### Target Release
TBD

### [REQ-DCW-06] MIG SHALL be able to allow users to upload DICOM objects via DICOMweb STOW-RS

#### Background

DICOMWeb STOW-RS enables storing instances over web protocols HTTP and secured connection via HTTPS; both protocols allow integration with other DICOMWeb enabled applications and services. By supporting DICOMWeb STOW-RS in the Informatics Gateway, users will be able to push DICOM instances to trigger an inference request.

#### Verification Strategy

Verify the DICOMWeb STOW-RS service allows POSTing studies with a DICOMWeb STOW-RS client, and the studies are uploaded to trigger a new workflow request.

#### Target Release

MONAI Deploy Informatics Gateway 0.2.0


### [REQ-DCW-07] MIG SHALL support exporting data to multiple DICOMweb destinations

#### Background

In some scenarios, users may want to export results to multiple DICOMweb destinations, and MIG must allow the user to configure multiple DICOMweb destinations.

#### Verification Strategy

Configure multiple DICOMweb destinations and make sure that MIG can reach them.

#### Target Release

MONAI Deploy Informatics Gateway R2

---

## EHR (EHR) Requirements

### [REQ-EHR-01] MIG SHALL be able to retrieve FHIR resources

#### Background
The FHIR client enables users to specify FHIR resources to be retrieved EHR data from an FHIR server using the ACR-DSI API.

#### Verification Strategy
Retrieve FHIR resources from an FHIR server.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ-EHR-02] MIG SHALL support exporting data to multiple FHIR destinations

#### Background

In some scenarios, users may want to export results to multiple FHIR destinations, and MIG must allow the user to configure multiple FHIR destinations.

#### Verification Strategy

Configure multiple FHIR destinations and make sure that MIG can reach them.

#### Target Release

MONAI Deploy Informatics Gateway R2

---

## Inference (INF) API Requirements

### [REQ-INF-01] MIG SHALL allow users to trigger jobs via standard APIs

#### Background

Often users would like to trigger an inference request using data from different data sources.  However, it is often not easy as there are many protocols in the healthcare industry. Furthermore, when combined with environmental constraints, it is much more difficult to manage how data is delivered and the timing/availability of the dataset.  This API allows users to specify multiple data sources and different data formats to be retrieved and organized into a single payload.

#### Verification Strategy
Test by using the APIs and make sure data is retrieved as specified, application(s) is/are executed as configured, and outputs the specified targets.

#### Target Release
MONAI Deploy Informatics Gateway R1

---

## Logging (LOG) Requirements

### [REQ-LOG-01] MIG SHALL log the DIMSE dataset

#### Background

Often time, data becomes corrupted during transport. By logging the DIMSE dataset, users would be able to inspect the content of each DICOM object.

#### Verification Strategy

Send a DICOM object and verify that the DIMSE dataset is logged in the log files.

#### Target Release

MONAI Deploy Informatics Gateway R1

### [REQ-LOG-02] Anonymize logs

MIG SHALL be able to hide any PHI in the logs.

#### Background

Medical data often contain very sensitive and patients' personal information. Therefore, MIG must be HIPAA compliant and avoid storing any information that can be traced to a particular patient to protect their privacy.

#### Verification Strategy

Verify that no logs include any PHI or any information that can be traced back to a particular person.

#### Target Release
MONAI Deploy Informatics Gateway R2

### [REQ-LOG-03] MIG SHOULD allow users to adjust log levels.

#### Background

Software applications often run into unexpected states, and verbose/trace logs are useful for administrators or engineers to troubleshoot these issues.  

#### Verification Strategy

Adjust to different log levels and verify that the log file contains only log entries that pertain to the configured log level.

#### Target Release

MONAI Deploy Informatics Gateway R1

---

## Functional (FNC) Requirements



### [REQ-FNC-01] Configurable retry logic

MIG SHALL allow users to configure the maximum number of retries and delays between each retry when communicating with external systems/services.

#### Background

Often, DICOM query and retrieve may fail because some modalities do not send the complete study to PACS altogether.  Allowing users to configure retry logic could help reduce users having to resend the entire study manually.

#### Verification Strategy

Configure a data source, such as Orthanc, and make it unavailable after sending the inference request.  Monitor the logs and then start Orthanc after failure(s).  Verify that the specified dataset is retrieved entirely.

#### Target Release

MONAI Deploy Informatics Gateway R1


### [REQ-FNC-02] MIG SHALL notify other subsystems when data is ready for processing

#### Background

Since MIG is the integration point with the medical systems for MONAI Deploy, it shall notify other subsystems when data is received and ready.


#### Verification Strategy

Send a dataset to MIG and expect MIG to emit a message to notify other subsystems of the location of the data.

#### Target Release

MONAI Deploy Informatics Gateway R1

### [REQ-FNC-03] MIG SHALL wait for data to arrive before submitting a job

#### Background

Given that DICOM dataset may not usually arrive at the system in a single association and some inference models may require data from multiple resources.  This requirement enables the users to configure how long MIG shall wait for data to arrive before submitting a request.

#### Verification Strategy

Configure MIG with different wait times and send a study in multiple associations.  Then, verify that the number of requests submitted by MIG is correct.

#### Target Release

MONAI Deploy Informatics Gateway R1

### [REQ-FNC-04] MIG SHALL make DICOM data available to other subsystems by grouping them into patient, study, or series

#### Background

Inference models usually operate on a DICOM study or a DICOM series. Therefore, MIG shall group incoming DICOM data and other data types into a patient, a study, or a series whenever possible before submitting a request.

#### Verification Strategy

Configure MIG to use different grouping and send multiple DICOM datasets to the system.  Expect MIG to group data by the configured option when submitting requests.

#### Target Release

MONAI Deploy Informatics Gateway R1

### [REQ-FNC-05] MIG SHALL notify users of system events

#### Background

Given that Informatics Gateway depends on several external services that may likely be unavailable due to unexpected circumstances,
the system shall actively notify users when such events occur. In addition, other non-critical system events and statistical data may also be collected and reported to the users.

#### Verification Strategy

Setup notification service, make one of the dependencies unavailable, and expect Informatics Gateway to notify the subscribers.

#### Target Release

MONAI Deploy Informatics Gateway R2
