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
Feature: Health Level 7

    @messaging_workflow_request @messaging
    Scenario Outline: Ability to store different versions of HL7 messages
        Given HL7 messages in version <version>
        When the message are sent to Informatics Gateway
        Then acknowledgement are received 
        And a workflow requests sent to message broker
        And messages are uploaded to storage service

        Examples:
            | version |
            | 2.3     |
            | 2.3.1   |
            | 2.4     |
            | 2.5.1   |
            | 2.6     |
            | 2.8     |
            
    @messaging_workflow_request @messaging @retry(10)
    Scenario Outline: Ability to receive and store multiple messages in a single batch
        Given HL7 messages in version <version>
        When the message are sent to Informatics Gateway in one batch
        Then acknowledgement are received 
        And a workflow requests sent to message broker
        And messages are uploaded to storage service

        Examples:
            | version |
            | 2.3     |
            | 2.3.1   |
            | 2.4     |
            | 2.5.1   |
            | 2.6     |
            | 2.8     |