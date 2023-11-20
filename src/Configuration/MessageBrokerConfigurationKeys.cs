/*
 * Copyright 2021-2022 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class MessageBrokerConfigurationKeys
    {
        /// <summary>
        /// Gets or sets the topic for publishing workflow requests.
        /// Defaults to `md_workflow_request`.
        /// </summary>
        [ConfigurationKeyName("workflowRequest")]
        public string WorkflowRequest { get; set; } = "md.workflow.request";

        /// <summary>
        /// Gets or sets the topic for publishing export complete requests.
        /// Defaults to `md_export_complete`.
        /// </summary>
        [ConfigurationKeyName("exportComplete")]
        public string ExportComplete { get; set; } = "md.export.complete";

        /// <summary>
        /// Gets or sets the topic for publishing export requests.
        /// Defaults to `md_export_request`.
        /// </summary>
        [ConfigurationKeyName("exportRequestPrefix")]
        public string ExportRequestPrefix { get; set; } = "md.export.request";

        /// <summary>
        /// Gets or sets the topic for publishing artifact recieved events.
        /// Defaults to `md_workflow_artifactrecieved`.
        /// </summary>
        [ConfigurationKeyName("artifactrecieved")]
        public string ArtifactRecieved { get; set; } = "md.workflow.artifactrecieved";


        /// <summary>
        /// Gets or sets the topic for publishing export requests.
        /// Defaults to `md_export_request`.
        /// </summary>
        [ConfigurationKeyName("externalAppRequest")]
        public string ExternalAppRequest { get; set; } = "md.externalapp.request";
    }
}
