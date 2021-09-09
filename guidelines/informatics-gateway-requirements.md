# MONAI Deploy Informatics Gateway Requirements

## Overview

The MONAI Deploy Informatics Gateway (MIG) is the integration point between healthcare information systems and the MONAI Deploy platform.  

## Purpose

This document defines the requirements for the MONAI Deploy Informatics Gateway.

## Goal
The goal for this proposal is to enlist, prioritize and provide clarity on the requirements for MONAI Deploy Informatics Gateway. Developers working on different software modules in MONAI Deploy Informatics Gateway SHALL use this specification as a guideline when designing and implementing software for the MONAI Deploy Informatics Gateway.

## Standard Language
This document SHALL follow the guidance of [rfc
2119](https://datatracker.ietf.org/doc/html/rfc2119) for terminology.

## Success Criteria
Users SHALL be able to send and receive data using standards defined by the healthcare industry including, but not limited to, [DICOM](https://www.dicomstandard.org/) and [FHIR](https://hl7.org/FHIR/).


## Attributes of a Requirement
For each requirement, the following attributes have been specified

**Requirement Body**: This is the text of the requirement which describes the goal and purpose behind the requirement.

**Background**: Provides necessary background to understand the context of the requirements.

**Verification Strategy**: A high level plan on how to test this requirement at a system level.

**Target Release**: Specifies which release of the MONAI App SDK this requirement is targeted for.

---

## DICOM Requirements

### [REQ] MIG SHALL respond to Verification Requests (C-ECHO)
A DICOM protocol similar to “ping” which allows remote to check the status of MONAI Deploy Informatics Gateway.

#### Background
C-ECHO is often used to test a connection between two DICOM endpoints after setup or when connection issues arises.

#### Verification Strategy
Use a C-ECHO SCU to verify that the C-ECHO SCP service responds.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] MIG SHALL respond to DICOM Store Requests (C-STORE)
MIG SHALL allow DICOM data to be received and stored via standard DICOM DIMSE C-STORE command.

#### Background
C-STORE SCP enables receiving of DICOM files with other DICOM devices.

#### Verification Strategy
Use a C-STORE SCU to send DICOM files to MIG's SCP.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] MIG SHALL be able to export DICOM via C-STORE
MIG SHALL allow DICOM data to be sent via standard DICOM DIMSE C-STORE command. This allows sending processed results to configured remote DICOM entities.

#### Background
Applications and/or algorithms often produce results in DICOM formats, e.g. a Structured Report (SR), that requires to be exported back for radiologists to review.  The C-STORE SCU service enabled sending DICOM files to other DICOM devices.

#### Verification Strategy
Use a C-STORE SCP as a listener to receive DICOM files from MIG's SCU.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] MIG SHOULD support Storage Commitment
MIG should support DICOM Storage Commitment service before deleting any associated data.

#### Background
Since MoNAI Deploy does not offer long term storage of processed data (results), a storage commitment message ensures that the target server commits for safekeeping the data exported before removing from MONAI Deploy platform.

#### Verification Strategy
TBD

#### Target Release
TBD

### [REQ] MIG SHALL be able to query external DICOM devices (C-FIND)
MIG SHALL be able to issue C-FIND requests to query external DICOM devices, such as PACS.

#### Background
The C-FIND service combines with the C-MOVE service would enable user to trigger inference requests, e.g. by using ACR-DSI API, and have MIG retrieve the DICOM studies for processing.

#### Verification Strategy
TBD

#### Target Release
TBD

### [REQ] MIG SHALL be able to retrieve DICOM instances from external DICOM devices (C-MOVE)
MIG SHALL be able to issue C-MOVE requests to retrieve DICOM instances from external DICOM devices, such as PACS.

#### Background
The C-FIND service combines with the C-MOVE service would enable user to trigger inference requests, e.g. by using ACR-DSI API, and have MIG retrieve DICOM studies for processing.

#### Verification Strategy
TBD

#### Target Release
TBD

---

## DICOMweb Requirements

### [REQ] MIG SHALL be able to query DICOMweb services (QIDO-RS)
MIG SHALL be able to act as a DICOMweb client and search DICOM objects via QIDO-RS.

#### Background
The QIDO-RS client enables MIG to query DICOMweb servers by specifying Accession Number, Patient and/or other DICOM tags.

#### Verification Strategy
Query a DICOMweb server using the client and ensure that results matches the input criteria.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] MIG SHALL be able to retrieve from DICOMweb services (WADO-RS)
MIG SHALL be able to act as a DICOMweb client and retrieve DICOM objects via WADO-RS.

#### Background
When WADO-RS is combined with an RESTful API, the users could easily trigger an inference request by specifying the DICOM studies without having to worry about setting up DICOM DIMSE services.

#### Verification Strategy
Retrieve DICOM data from a DICOMweb server.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] MIG SHALL be able to export to DICOMweb services (STOW-RS)
MIG SHALL be able to act as a DICOMweb client and export DICOM objects via STOW-RS.

#### Background
The STOW-RS client allows MIG to export DICOM results to external DICOMweb servers.

#### Verification Strategy
Export a DICOM object to a DICOMweb server using the DICOMweb STOW client.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] MIG SHALL be able to accept DICOMweb query requests (QIDO-RS)
MIG SHALL be able to act as a DICOMweb server and allow users to search available DICOM objects hosted by the MONAI App Server.

#### Background
TBD
#### Verification Strategy
TBD
#### Target Release
TBD

### [REQ] MIG SHALL be able to allow users to retrieve DICOM objects via DICOMweb (WADO-RS)
MIG SHALL be able to act as a DICOMweb server and allow users to download DICOM objects hosted by the MONAI App Server.

#### Background
TBD
#### Verification Strategy
TBD
#### Target Release
TBD

### [REQ] MIG SHALL be able to allow users to upload DICOM objects via DICOMweb STOW-RS
MIG SHALL be able to act as a DICOMweb server and allow users to upload DICOM objects.

#### Background
TBD
#### Verification Strategy
TBD
#### Target Release
TBD

---

## FHIR Requirements

### [REQ] MIG SHOULD be able to retrieve FHIR resources
MIG SHALL be able to retrieve and store FHIR resources requested by the users.

#### Background
The FHIR client enables users to specify FHIR resources to be retrieved EHR data from a FHIR server using the ACR-DSI API.

#### Verification Strategy
Retrieve FHIR resources from a FHIR server.

#### Target Release
MONAI Deploy Informatics Gateway R1

---

## Inference API Requirements

### [REQ] MIG SHALL allow users to trigger jobs via standard APIs
MIG SHALL allow users to trigger jobs via APIs.

#### Background
Often users would like to trigger an application using data from different data sources.  However, it cannot be easily done given that there may be different protocols, environment constraints of how data is delivered or just timing/availability of the dataset.  This API allows user to specify multiple data sources, different formats of data to be retrieved and organized into a single payload.

#### Verification Strategy
Test by using the APIs and make sure data is retrieved as specified, application(s) is/are executed as configured and outputs the the specified targets.

#### Target Release
MONAI Deploy Informatics Gateway R1

---

## Logging Requirements

### [REQ] Log DIMSE dataset
MIG SHALL log DIMSE dataset if enabled.

#### Background
Often time, data becomes corrupted during transport. By logging DIMSE dataset, users would be able to inspect the content of each DICOM object.

#### Verification Strategy
Send a DICOM object and verify that DIMSE dataset is logged.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] Anonymize logs
MIG SHALL be able to hide any PHI in the logs.

#### Background
Medical data often contain very sensitive and patients' personal information.  MIG must be HIPAA compliant and avoid storing any information that can be traced to a particular patient to protect their privacy.

#### Verification Strategy
Verify that no logs include any PHI or any piece of information can be used to trace back to a particular person.

#### Target Release
MONAI Deploy Informatics Gateway R2

### [REQ] Log Levels
MIG SHOULD allow users to adjust log levels.

#### Background
Software applications often run into unexpected states and verbose/trace logs are useful for administrators or engineers to troubleshoot these issues.  

#### Verification Strategy
Adjust to different log levels and verify that the log file contains only log entries pertain to the configured log level.

#### Target Release
MONAI Deploy Informatics Gateway R1

---

## Configuration Requirements
### [REQ] Store SCP AE Title shall be configurable
MIG SHALL allow users to configure the SCP AE TItle for Clara DICOM Adapter.

#### Background
Administrators may often name the AE Title based on geography of the device or purpose of the device because there may be multiple instances.  This would allow them to change the default value according to their needs and avoid confusion.

#### Verification Strategy
Verify that only the configured AE Title responds to the association request.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] Listening port shall be configurable
MIG SHALL allow users to configure the port for the DICOM TCP listening port.

#### Background
There may often be many applications listening on different ports on the same system which could result in a conflict.  Therefore, allowing users to change the listening port for the SCP is a must.

#### Verification Strategy
Change the listening port of the SCP and verify that it could still receive C-ECHO requests.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] Verification protocol shall be configurable
MIG SHALL handle or ignore C-ECHO requests based on user configuration. 

#### Background
Often users may want to disable response to C-ECHO requests similar to disabling responding to ICMP (ping) commands to avoid network attacks.

#### Verification Strategy
Disable C-ECHO and verify that the SCP no longer responds to the C-ECHO requests but still accept C-STORE requests.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] Store SCU AE Title shall be configurable
MIG SHALL allow users to configure the SCU AE TItle.

#### Background
Administrators may often name the AE Title based on geography of the device or purpose of the device because there may be multiple instances.  This would allow them to change the default value according to their needs and avoid confusion.

#### Verification Strategy
Change the SCU AE Title and verify that the export destination is responding to the updated AE Title.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] Allow multiple remote (source) SCP to be configured
MIG SHALL allow users to configure multiple remote (source) AETs.

#### Background
There are often more than one DICOM devices in a hospital and MIG may often need to accept data from multiple DICOM devices. Therefore, MIG must allow users to configure multiple DICOM sources.

#### Verification Strategy
Configure multiple DICOM sources and make sure that all DICOM sources can send data to MIG.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] Allow multiple remote (destination) SCP to be configured
MIG SHALL allow users to configure multiple remote (destination) AETs.

#### Background
In some scenarios, results may need to be exported to multiple DICOM destinations and MIG must allow the user to configure multiple DICOM destinations.

#### Verification Strategy
Configure multiple DICOM destinations and make sure that all DICOM destinations can be reached.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] Configure number of concurrent associations
MIG SHALL allow user to configure number of concurrent association that are allowed, per system.

#### Background
Often time, DICOM devices, such as PACS, may initiate multiple associations concurrently to deliver a DICOM dataset.  Therefore, MIG must be able to accept concurrent associations.

#### Verification Strategy
Launch multiple store SCUs to send data to MIG and verify that all data has been received.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] Received data from known DICOM sources
MIG SHALL accept DICOM objects from known DICOM sources and reject if not configured.

#### Background
To prevent receiving data from unknown DICOM sources, the system must be able to identify and validate based on the caller's identity, such as, caller's AE Title, IP address, etc...

#### Verification Strategy
Send a C-ECHO request to MIG using an AE Title that is not configured on MIG and verify the the response is rejected with an appropriate reason.

#### Target Release
MONAI Deploy Informatics Gateway R1

### [REQ] Configurable retry logic
MIG SHALL allow user to configure the maximum number of retries and delays in-between each retry when communicating with external systems/services.

#### Background
Often time, DICOM query and retrieve may fail due to the fact that some modalities do not send the complete study to PACS altogether.  Allowing users to configure retry logic could help reduce users having to resend the entire study manually.

#### Verification Strategy
Configure a data source, such as Orthanc, and make it unavailable after sending the inference request.  Monitor the logs and then start Orthanc after failure(s).  Verify that the specified dataset is retrieved entirely.

#### Target Release
MONAI Deploy Informatics Gateway R1
