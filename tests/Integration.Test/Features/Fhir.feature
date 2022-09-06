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
Feature: FHIR

    @messaging_workflow_request @messaging
    Scenario Outline: Ability to store FHIR messages in different formats
        Given FHIR message <version> in <format>
        When the FHIR messages are sent to Informatics Gateway
        Then workflow requests are sent to message broker
        And FHIR resources are uploaded to storage service

        Examples:
            | version     | format |
            | 1.0.2 DSTU2 | XML    |
            | 1.0.2 DSTU2 | JSON   |
            | 3.0.2 STU3  | XML    |
            | 3.0.2 STU3  | JSON   |
            | 4.0.1 R4    | XML    |
            | 4.0.1 R4    | JSON   |
            | 4.3.0 R4B   | XML    |
            | 4.3.0 R4B   | JSON   |
