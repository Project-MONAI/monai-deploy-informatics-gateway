# SPDX-FileCopyrightText: © 2022 MONAI Consortium
# SPDX-License-Identifier: Apache License 2.0

@scp
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
