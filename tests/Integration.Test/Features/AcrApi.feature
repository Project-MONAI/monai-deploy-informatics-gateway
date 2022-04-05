# SPDX-FileCopyrightText: © 2022 MONAI Consortium
# SPDX-License-Identifier: Apache License 2.0

Feature: ACR API

    This feature tests the DIMSE services provided by the Informatics Gateway as a SCP.
    Requirements covered:
    - [REQ-DCW-01] MIG SHALL be able to query DICOMweb services (QIDO-RS)
    - [REQ-DCW-02] MIG SHALL be able to retrieve from DICOMweb services via WADO-RS
    - [REQ-EHR-01] MIG SHALL be able to retrieve FHIR resources
    - [REQ-INF-01] MIG SHALL allow users to trigger jobs via standard APIs


    @messaging_workflow_request @messaging
    Scenario: ACR w/ DICOMweb Q&R
        Given a DICOM study on a remote DICOMweb service
        And an ACR API request to query & retrieve by <requestType>
        When the ACR API request is sent
        Then a workflow requests sent to the message broker
        And a study is uploaded to the storage service
        And the temporary data directory is cleared

    Examples:
        | requestType     |
        | Study           |
        | Patient         |
        | AccessionNumber |
