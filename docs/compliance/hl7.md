<!--
  ~ Copyright 2022 MONAI Consortium
  ~
  ~ Licensed under the Apache License, Version 2.0 (the "License");
  ~ you may not use this file except in compliance with the License.
  ~ You may obtain a copy of the License at
  ~
  ~ http://www.apache.org/licenses/LICENSE-2.0
  ~
  ~ Unless required by applicable law or agreed to in writing, software
  ~ distributed under the License is distributed on an "AS IS" BASIS,
  ~ WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  ~ See the License for the specific language governing permissions and
  ~ limitations under the License.
-->

# HL7 (Health Level 7)

The following reference describes the connectivity capabilities of MONAI Deploy SDK out of the box.
Users implementing the MONAI Deploy SDK must update their HL7 Conformance Statement according
to the actual capabilities of their application.

## HL7 MLLP Listener

The HL7 listener adheres to the Minimal Lower Layer Protocol (MLLP) which accepts Health Level 7
messages via TCP/IP connections at port 2575 (default). The Informatics Gateway provides basic syntax validation
based on [HL7-dotnetcore](https://github.com/Efferent-Health/HL7-dotnetcore) toolkit. All validated messages are stored
regardless of the message types.

### Communication

The Informatics Gateway allows concurrent connections for the HL7 listener and allows multiple messages to be transmitted
in a single connection or in a single command. Messages received are immediately assembled into a payload for the 
[MONAI Deploy Workflow Manager](https://github.com/Project-MONAI/monai-deploy-workflow-manager) to consume.

### Configurations

The HL7 listener can be configured in the `appsettings.json` file. Refer to [Configuration](../setup/schema.md) for additional details.

### Acknowledgement Mode

The listener supports the acknowledgment mode dictated in the MSH.15 field. If no value is specified, the listener defaults to the configuration option `sendAck`.

### Supported Character Sets

The HL7 listener supports Unicode UTF8 or any 8-bit character sets.
