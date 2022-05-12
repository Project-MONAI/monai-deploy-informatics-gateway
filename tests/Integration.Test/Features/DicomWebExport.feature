# SPDX-FileCopyrightText: © 2022 MONAI Consortium
# SPDX-License-Identifier: Apache License 2.0

# @ignored
Feature: DICOMweb Export Service

    This feature tests the DICOMweb export services provided by the Informatics Gateway.

    Requirements covered:
    - [REQ-DCW-03] MIG SHALL be able to export to DICOMweb services via STOW-RS
    - [REQ-DCW-07] MIG SHALL support exporting data to multiple DICOMweb destinations

    @messaging_export_complete @messaging @sql_inject_acr_request
    Scenario: Export to a DICOMweb service
        Given 1 <modality> studies for export
        When a export request is sent for 'md.export.request.monaidicomweb'
        Then Informatics Gateway exports the studies to Orthanc

        Examples:
            | modality |
            | MR       |
            | CT       |
            | MG       |
            | US       |
            | Tiny     |
