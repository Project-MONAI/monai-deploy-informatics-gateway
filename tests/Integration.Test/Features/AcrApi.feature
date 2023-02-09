# Copyright 2022 MONAI Consortium
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
        Then a single workflow request is sent to the message broker
        And a study is uploaded to the storage service

    Examples:
        | requestType     |
        | Study           |
        | Patient         |
        | AccessionNumber |
