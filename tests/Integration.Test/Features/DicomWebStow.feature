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
Feature: DICOMweb STOW-RS Service

    This feature tests the DICOMweb STOW-rS services provided by the Informatics Gateway.

    Requirements covered:
    - [STOW-RS] MIG SHALL be able to allow users to upload DICOM objects via DICOMweb STOW-RS

    @messaging_workflow_request @messaging @dicomweb_stow
    Scenario: Triggers a new workflow request via DICOMWeb STOW-RS
        Given <count> <modality> studies
        When the studies are uploaded to the DICOMWeb STOW-RS service at '/dicomweb/'
        Then 1 workflow requests sent to message broker
        And studies are uploaded to storage service
        Examples:
            | modality | count |
            | MR       | 1     |
            | MG       | 2     |

    @messaging_workflow_request @messaging @dicomweb_stow_study
    Scenario: Triggers a new workflow with given study instance UID request via DICOMWeb STOW-RS
        Given <count> <modality> studies
        When the studies are uploaded to the DICOMWeb STOW-RS service at '/dicomweb/' with StudyInstanceUid
        Then 1 workflow requests sent to message broker
        And studies are uploaded to storage service
        Examples:
            | modality | count |
            | CT       | 2     |
            | US       | 1     |

    @messaging_workflow_request @messaging @dicomweb_stow
    Scenario: Triggers a new workflow via DICOMWeb STOW-RS
        Given <count> <modality> studies
        And a workflow named 'MyWorkflow'
        When the studies are uploaded to the DICOMWeb STOW-RS service at '/dicomweb/'
        Then 1 workflow requests sent to message broker
        And studies are uploaded to storage service
        Examples:
            | modality | count |
            | MR       | 2     |
            | US       | 1     |

    @messaging_workflow_request @messaging @dicomweb_stow_study
    Scenario: Triggers a specific workflow with given study instance UID request via DICOMWeb STOW-RS
        Given <count> <modality> studies
        And a workflow named 'MyWorkflow'
        When the studies are uploaded to the DICOMWeb STOW-RS service at '/dicomweb/' with StudyInstanceUid
        Then 1 workflow requests sent to message broker
        And studies are uploaded to storage service
        Examples:
            | modality | count |
            | MR       | 2     |
            | US       | 1     |
