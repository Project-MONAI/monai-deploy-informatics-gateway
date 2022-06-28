# SPDX-FileCopyrightText: © 2022 MONAI Consortium
# SPDX-License-Identifier: Apache License 2.0

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
        And the temporary data directory has been cleared
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
        And the temporary data directory has been cleared
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
        And the temporary data directory has been cleared
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
        And the temporary data directory has been cleared
        Examples:
            | modality | count |
            | MR       | 2     |
            | US       | 1     |
