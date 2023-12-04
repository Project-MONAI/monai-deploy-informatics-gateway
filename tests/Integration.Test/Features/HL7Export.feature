
# Copyright 2023 MONAI Consortium
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

Feature: HL7 Export

This feature tests the Export Hl7.

    @messaging_workflow_request @messaging
    Scenario: End-to-end test of HL7 exporting
        Given a HL7 message that is exported to the test host
        When the HL7 Export message is received with 6 messages acked true
        Then ensure that exportcomplete messages are sent with success


    Scenario: End-to-end test of HL7 exporting with no Ack
        Given a HL7 message that is exported to the test host
        When the HL7 Export message is received with 6 messages acked false
        Then ensure that exportcomplete messages are sent with failure
