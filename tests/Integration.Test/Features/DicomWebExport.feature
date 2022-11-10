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
Feature: DICOMweb Export Service

    This feature tests the DICOMweb export services provided by the Informatics Gateway.

    Requirements covered:
    - [REQ-DCW-03] MIG SHALL be able to export to DICOMweb services via STOW-RS
    - [REQ-DCW-07] MIG SHALL support exporting data to multiple DICOMweb destinations

    @messaging_export_complete @messaging 
    Scenario: Export to a DICOMweb service
        Given an ACR request in the database
        And 1 <modality> studies for export
        When a export request is sent for 'md.export.request.monaidicomweb'
        Then Informatics Gateway exports the studies to Orthanc

        Examples:
            | modality |
            | MR       |
            | CT       |
            | MG       |
            | US       |
            | Tiny     |
