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

Feature: Remote App Execution Plug-in

This feature tests the Remote App Execution plug-ins for de-identifying and
re-identifying data sent and received by the MIG respectively.

    
    @messaging_workflow_request @messaging
    Scenario: End-to-end test of plug-ins
        Given a study that is exported to the test host
        When the study is received and sent back to Informatics Gateway
        Then ensure the original study and the received study are the same
