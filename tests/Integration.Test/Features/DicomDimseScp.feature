# SPDX-FileCopyrightText: © 2022 MONAI Consortium
# SPDX-License-Identifier: Apache License 2.0

# @ignored
Feature: DICOM DIMSE SCP Services

    This feature tests the DIMSE services provided by the Informatics Gateway as a SCP.
    Requirements covered:
    - [REQ-DCM-01] MIG SHALL respond to Verification Requests (C-ECHO)
    - [REQ-DCM-02] MIG SHALL respond to DICOM Store Requests (C-STORE)
    - [REQ-DCM-06] MIG SHALL allow users to configure the SCP AE TItle
    - [REQ-DCM-10] MIG MUST accept DICOM data from multiple sources
    - [REQ-FNC-02] MIG SHALL notify other subsystems when data is ready for processing
    - [REQ-FNC-03] MIG SHALL wait for data to arrive before submitting a job
    - [REQ-FNC-04] MIG SHALL make DICOM data available to other subsystems by grouping them into patient, study, or series
    - [REQ-FNC-05] MIG SHALL notify users of system events

    Background: Setup AE Titles
        Given a calling AE Title 'TEST-RUNNER'

    Scenario: Response to C-ECHO-RQ
        Given a called AE Title named 'C-ECHO-TEST' that groups by '0020,000D' for 5 seconds
        When a C-ECHO-RQ is sent to 'C-ECHO-TEST' from 'TEST-RUNNER' with timeout of 30 seconds
        Then a successful response should be received

    @messaging_workflow_request @messaging
    Scenario Outline: Respond to C-STORE-RQ and group data by Study Instance UID
        Given a called AE Title named 'C-STORE-STUDY' that groups by '0020,000D' for 3 seconds
        And <count> <modality> studies
        When a C-STORE-RQ is sent to 'Informatics Gateway' with AET 'C-STORE-STUDY' from 'TEST-RUNNER' with timeout of 300 seconds
        Then a successful response should be received
        And <count> workflow requests sent to message broker
        And studies are uploaded to storage service
        And the temporary data directory has been cleared

        Examples:
            | modality | count |
            | MR       | 1     |
            | CT       | 1     |
            | MG       | 2     |
            | US       | 1     |

    @messaging_workflow_request @messaging
    Scenario Outline: Respond to C-STORE-RQ and group data by Series Instance UID
        Given a called AE Title named 'C-STORE-SERIES' that groups by '0020,000E' for 3 seconds
        And <study_count> <modality> studies with <series_count> series per study
        When a C-STORE-RQ is sent to 'Informatics Gateway' with AET 'C-STORE-SERIES' from 'TEST-RUNNER' with timeout of 300 seconds
        Then a successful response should be received
        And <series_count> workflow requests sent to message broker
        And studies are uploaded to storage service
        And the temporary data directory has been cleared

        Examples:
            | modality | study_count | series_count |
            | MR       | 1           | 2            |
            | CT       | 1           | 2            |
            | MG       | 1           | 3            |
            | US       | 1           | 2            |
