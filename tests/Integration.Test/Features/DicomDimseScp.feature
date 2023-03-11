# Copyright 2022-2023 MONAI Consortium
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
# http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

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
        And a DICOM client configured with 30 seconds timeout
        When a C-ECHO-RQ is sent to 'C-ECHO-TEST' from 'TEST-RUNNER'
        Then a successful response should be received

    @messaging_workflow_request @messaging
    Scenario Outline: Respond to C-STORE-RQ and group data by Study Instance UID
        Given a called AE Title named '<aet>' that groups by '0020,000D' for <timeout> seconds
        And a DICOM client configured with 300 seconds timeout
        And a DICOM client configured to send data over 1 associations and wait 0 between each association
        And <count> <modality> studies
        When a C-STORE-RQ is sent to 'Informatics Gateway' with AET '<aet>' from 'TEST-RUNNER'
        Then a successful response should be received
        And <count> workflow requests sent to message broker
        And studies are uploaded to storage service

        Examples:
            | modality | count | aet             | timeout |
            | MR       | 1     | C-STORE-STUDY30 | 3      |
            | CT       | 1     | C-STORE-STUDY30 | 3      |
            | MG       | 2     | C-STORE-STUDY10 | 3      |
            | US       | 1     | C-STORE-STUDY10 | 3      |

    @messaging_workflow_request @messaging
    Scenario Outline: Respond to C-STORE-RQ and group data by Series Instance UID
        Given a called AE Title named '<aet>' that groups by '0020,000E' for <timeout> seconds
        And a DICOM client configured with 300 seconds timeout
        And a DICOM client configured to send data over 1 associations and wait 0 between each association
        And <study_count> <modality> studies with <series_count> series per study
        When a C-STORE-RQ is sent to 'Informatics Gateway' with AET '<aet>' from 'TEST-RUNNER'
        Then a successful response should be received
        And <series_count> workflow requests sent to message broker
        And studies are uploaded to storage service

        Examples:
            | modality | study_count | series_count | aet           | timeout |
            | MR       | 1           | 2            | C-STORE-SER30 | 3      |
            | CT       | 1           | 2            | C-STORE-SER30 | 3      |
            | MG       | 1           | 3            | C-STORE-SER10 | 3      |
            | US       | 1           | 2            | C-STORE-SER10 | 3      |
            
    @messaging_workflow_request @messaging
    Scenario Outline: Respond to C-STORE-RQ and group data by Study Instance UID over multiple associations
        Given a called AE Title named 'C-STORE-MA' that groups by '0020,000D' for 5 seconds
        And a DICOM client configured with 300 seconds timeout
        And a DICOM client configured to send data over <series_count> associations and wait <seconds> between each association
        And <study_count> <modality> studies with <series_count> series per study
        When C-STORE-RQ are sent to 'Informatics Gateway' with AET 'C-STORE-MA' from 'TEST-RUNNER'
        Then a successful response should be received
        And <workflow_requests> workflow requests sent to message broker
        And studies are uploaded to storage service

        Examples:
            | modality | study_count | series_count | seconds | workflow_requests |
            | MG       | 1           | 3            | 3       | 1                 |
            | MG       | 1           | 3            | 6       | 3                 |
