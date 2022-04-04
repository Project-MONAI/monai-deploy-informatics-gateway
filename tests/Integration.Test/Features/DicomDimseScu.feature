# Copyright 2022 MONAI Consortium
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#     http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

@scp @ignore
Feature: DICOM DIMSE SCU Services

    This feature tests the DIMSE services provided by the Informatics Gateway as a SCU.
    Requirements covered:
    - [REQ-DCM-03] MIG SHALL be able to export DICOM via C-STORE
    - [REQ-DCM-09] Store SCU AE Title shall be configurable
    - [REQ-DCM-11] MIG SHALL support exporting data to multiple DICOM destinations

    @messaging_export_complete @messaging
    Scenario: Export to a DICOM device
        Given a DICOM destination registered with Informatics Gateway
        And <count> <modality> studies for export
        When a export request is sent for 'md.export.request.monaiscu'
        Then Informatics Gateway exports the studies to the DICOM SCP

        Examples:
            | modality | count |
            | MR       | 1     |
            | CT       | 1     |
            | MG       | 1     |
            | US       | 1     |
            | Tiny     | 1     |
