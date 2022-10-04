/*
 * Copyright 2022 MONAI Consortium
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

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.CLI.Services;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 30000, Level = LogLevel.Warning, Message = "{message}")]
        public static partial void WarningMessage(this ILogger logger, string message);

        [LoggerMessage(EventId = 30001, Level = LogLevel.Critical, Message = "{message}")]
        public static partial void CriticalException(this ILogger logger, string message);

        [LoggerMessage(EventId = 30002, Level = LogLevel.Debug, Message = "{messge}")]
        public static partial void DebugMessage(this ILogger logger, string messge);

        [LoggerMessage(EventId = 30003, Level = LogLevel.Critical, Message = "{message}")]
        public static partial void ConfigurationException(this ILogger logger, string message);

        [LoggerMessage(EventId = 30010, Level = LogLevel.Information, Message = "New MONAI Deploy SCP Application Entity created:\r\n\tName:     {name}\r\n\tAE Title:     {aeTitle}\r\n\tGrouping:     {grouping}\r\n\tTimeout:     {timeout}")]
        public static partial void MonaiAeTitleCreated(this ILogger logger, string name, string aeTitle, string grouping, uint timeout);

        [LoggerMessage(EventId = 30011, Level = LogLevel.Information, Message = "\tWorkflows: {workflows}")]
        public static partial void MonaiAeWorkflows(this ILogger logger, string workflows);

        [LoggerMessage(EventId = 30012, Level = LogLevel.Warning, Message = "Data received by this Application Entity will bypass Data Routing Service.")]
        public static partial void WorkflowWarning(this ILogger logger);

        [LoggerMessage(EventId = 30013, Level = LogLevel.Information, Message = "\tIgnored SOP Classes: {ignoredSopClasses}")]
        public static partial void MonaiAeIgnoredSops(this ILogger logger, string ignoredSopClasses);

        [LoggerMessage(EventId = 30014, Level = LogLevel.Warning, Message = "Instances with matching SOP class UIDs are accepted but dropped.")]
        public static partial void IgnoredSopClassesWarning(this ILogger logger);

        [LoggerMessage(EventId = 30015, Level = LogLevel.Critical, Message = "Error creating MONAI SCP AE Title {aeTitle}: {message}.")]
        public static partial void MonaiAeCreateCritical(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30017, Level = LogLevel.Critical, Message = "Error retrieving MONAI SCP AE Titles: {message}.")]
        public static partial void ErrorListingMonaiAeTitles(this ILogger logger, string message);

        [LoggerMessage(EventId = 30018, Level = LogLevel.Warning, Message = "No MONAI SCP Application Entities configured.")]
        public static partial void NoAeTitlesFound(this ILogger logger);

        [LoggerMessage(EventId = 30019, Level = LogLevel.Information, Message = "MONAI SCP AE Title '{aeTitle}' deleted.")]
        public static partial void MonaiAeTitleDeleted(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 30020, Level = LogLevel.Critical, Message = "Error deleting MONAI SCP AE Title {aeTitle}: {message}")]
        public static partial void ErrorDeletingMonaiAeTitle(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30021, Level = LogLevel.Information, Message = "Informatics Gateway API: {endpoint}")]
        public static partial void ConfigInformaticsGatewayApiEndpoint(this ILogger logger, string endpoint);

        [LoggerMessage(EventId = 30022, Level = LogLevel.Information, Message = "DICOM SCP Listening Port: {port}")]
        public static partial void ConfigDicomScpPort(this ILogger logger, int port);

        [LoggerMessage(EventId = 30023, Level = LogLevel.Information, Message = "Container Runner: {runner}")]
        public static partial void ConfigContainerRunner(this ILogger logger, Runner runner);

        [LoggerMessage(EventId = 30024, Level = LogLevel.Information, Message = "Host:\r\n\tDatabase storage mount: {database}\r\n\tData storage mount: {data}\r\n\tLogs storage mount: {log}")]
        public static partial void ConfigHostInfo(this ILogger logger, string database, string data, string log);

        [LoggerMessage(EventId = 30025, Level = LogLevel.Information, Message = "New DICOM destination created:\r\n\tName:     {name}\r\n\tAE Title:     {aeTitle}\r\n\tHost/IP Address:     {hostIp}\r\n\tPort:     {port}")]
        public static partial void DicomDestinationCreated(this ILogger logger, string name, string aeTitle, string hostIp, int port);

        [LoggerMessage(EventId = 30026, Level = LogLevel.Critical, Message = "Error creating DICOM destination {aeTitle}: {message}")]
        public static partial void ErrorCreatingDicomDestination(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30027, Level = LogLevel.Information, Message = "DICOM destination '{name}' deleted.")]
        public static partial void DicomDestinationDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 30028, Level = LogLevel.Critical, Message = "Error deleting DICOM destination {aeTitle}: {message}")]
        public static partial void ErrorDeletingDicomDestination(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30029, Level = LogLevel.Critical, Message = "Error retrieving DICOM destinations: {message}.")]
        public static partial void ErrorListingDicomDestinations(this ILogger logger, string message);

        [LoggerMessage(EventId = 30030, Level = LogLevel.Warning, Message = "No DICOM destinations configured.")]
        public static partial void NoDicomDestinationFound(this ILogger logger);

        [LoggerMessage(EventId = 30031, Level = LogLevel.Critical, Message = "Error creating DICOM source {aeTitle}: {message}")]
        public static partial void ErrorCreatingDicomSource(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30032, Level = LogLevel.Information, Message = "DICOM source '{name}' deleted.")]
        public static partial void DicomSourceDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 30033, Level = LogLevel.Critical, Message = "Error deleting DICOM source {aeTitle}: {message}")]
        public static partial void ErrorDeletingDicomSource(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30034, Level = LogLevel.Critical, Message = "Error retrieving DICOM sources: {message}.")]
        public static partial void ErrorListingDicomSources(this ILogger logger, string message);

        [LoggerMessage(EventId = 30035, Level = LogLevel.Warning, Message = "No DICOM sources configured.")]
        public static partial void NoDicomSourcesFound(this ILogger logger);

        [LoggerMessage(EventId = 30036, Level = LogLevel.Information, Message = "New DICOM source created:\r\n\tName:     {name}\r\n\tAE Title:     {aeTitle}\r\n\tHost/IP Address:     {hostIp}")]
        public static partial void DicomSourceCreated(this ILogger logger, string name, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 30037, Level = LogLevel.Critical, Message = "Error retrieving service status: {message}")]
        public static partial void ErrorRetrievingStatus(this ILogger logger, string message);

        [LoggerMessage(EventId = 30038, Level = LogLevel.Information, Message = "Number of active DIMSE connections: {connections}")]
        public static partial void StatusDimseConnections(this ILogger logger, int connections);

        [LoggerMessage(EventId = 30039, Level = LogLevel.Information, Message = "Service Status:")]
        public static partial void ServiceStatusHeader(this ILogger logger);

        [LoggerMessage(EventId = 30040, Level = LogLevel.Information, Message = "\t\t{name}: {status}")]
        public static partial void ServiceStatusItem(this ILogger logger, string name, ServiceStatus status);

        [LoggerMessage(EventId = 30041, Level = LogLevel.Warning, Message = "Action canceled.")]
        public static partial void ActionCancelled(this ILogger logger);

        [LoggerMessage(EventId = 30042, Level = LogLevel.Critical, Message = "Error restarting {applicationName}: {message}.")]
        public static partial void ErrorRestarting(this ILogger logger, string applicationName, string message);

        [LoggerMessage(EventId = 30043, Level = LogLevel.Debug, Message = "Available manifest names {names}.")]
        public static partial void AvailableManifest(this ILogger logger, string names);

        [LoggerMessage(EventId = 30044, Level = LogLevel.Information, Message = "Saving appsettings.json to {path}.")]
        public static partial void SaveAppSettings(this ILogger logger, string path);

        [LoggerMessage(EventId = 30045, Level = LogLevel.Information, Message = "{path} updated successfully.")]
        public static partial void AppSettingUpdated(this ILogger logger, string path);

        [LoggerMessage(EventId = 30046, Level = LogLevel.Debug, Message = "{applicationName} with container ID {id} running={isRunning}.")]
        public static partial void ApplicationStoppedState(this ILogger logger, string applicationName, string id, bool isRunning);

        [LoggerMessage(EventId = 30047, Level = LogLevel.Information, Message = "{applicationName} with container ID {id} stopped.")]
        public static partial void ApplicationStopped(this ILogger logger, string applicationName, string id);

        [LoggerMessage(EventId = 30048, Level = LogLevel.Warning, Message = "Error stopping {applicationName} with container ID {id}. Please verify with the application state with {runner}.")]
        public static partial void ApplicationStopError(this ILogger logger, string applicationName, string id, Runner runner);

        [LoggerMessage(EventId = 30049, Level = LogLevel.Information, Message = "Configuration updated successfully.")]
        public static partial void ConfigurationUpdated(this ILogger logger);

        [LoggerMessage(EventId = 30050, Level = LogLevel.Information, Message = "\tAccepted SOP Classes: {alowedSopClasses}")]
        public static partial void MonaiAeAllowedSops(this ILogger logger, string alowedSopClasses);

        [LoggerMessage(EventId = 30051, Level = LogLevel.Warning, Message = "Instances without matching SOP class UIDs are accepted but dropped.")]
        public static partial void AllowedSopClassesWarning(this ILogger logger);

        [LoggerMessage(EventId = 30052, Level = LogLevel.Warning, Message = "Only instances with matching SOP class UIDs are accepted and stored.")]
        public static partial void AcceptedSopClassesWarning(this ILogger logger);

        [LoggerMessage(EventId = 30053, Level = LogLevel.Information, Message = "\n\nFound {count} items.")]
        public static partial void ListedNItems(this ILogger logger, int count);

        [LoggerMessage(EventId = 30054, Level = LogLevel.Information, Message = "C-ECHO to {name} completed successfully.")]
        public static partial void DicomCEchoSuccessful(this ILogger logger, string name);

        [LoggerMessage(EventId = 30055, Level = LogLevel.Critical, Message = "C-ECHO to {name} failed: {error}.")]
        public static partial void ErrorCEchogDicomDestination(this ILogger logger, string name, string error);

        [LoggerMessage(EventId = 30056, Level = LogLevel.Information, Message = "DICOM destination updated:\r\n\tName:     {name}\r\n\tAE Title:     {aeTitle}\r\n\tHost/IP Address:     {hostIp}\r\n\tPort:     {port}")]
        public static partial void DicomDestinationUpdated(this ILogger logger, string name, string aeTitle, string hostIp, int port);

        [LoggerMessage(EventId = 30057, Level = LogLevel.Critical, Message = "Error updating DICOM destination {aeTitle}: {message}")]
        public static partial void ErrorUpdatingDicomDestination(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30058, Level = LogLevel.Information, Message = "DICOM source updated:\r\n\tName:     {name}\r\n\tAE Title:     {aeTitle}\r\n\tHost/IP Address:     {hostIp}")]
        public static partial void DicomSourceUpdated(this ILogger logger, string name, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 30059, Level = LogLevel.Critical, Message = "Error updating DICOM source {aeTitle}: {message}")]
        public static partial void ErrorUpdatingDicomSource(this ILogger logger, string aeTitle, string message);

        // Docker Runner
        [LoggerMessage(EventId = 31000, Level = LogLevel.Debug, Message = "Checking for existing {applicationName} ({version}) containers...")]
        public static partial void CheckingExistingAppContainer(this ILogger logger, string applicationName, string version);

        [LoggerMessage(EventId = 31001, Level = LogLevel.Debug, Message = "Connecting to Docker...")]
        public static partial void ConnectingToDocker(this ILogger logger);

        [LoggerMessage(EventId = 31002, Level = LogLevel.Debug, Message = "Retrieving images from Docker...")]
        public static partial void RetrievingImagesFromDocker(this ILogger logger);

        [LoggerMessage(EventId = 31003, Level = LogLevel.Information, Message = "Creating container {applicationName} - {version} ({id})...")]
        public static partial void CreatingDockerContainer(this ILogger logger, string applicationName, string version, string id);

        [LoggerMessage(EventId = 31004, Level = LogLevel.Information, Message = "\tPort binding: {port}/tcp")]
        public static partial void DockerPrtBinding(this ILogger logger, int port);

        [LoggerMessage(EventId = 31005, Level = LogLevel.Information, Message = "\tMount (configuration file): {hostPath} => {containerPath}")]
        public static partial void DockerMountConfigFile(this ILogger logger, string hostPath, string containerPath);

        [LoggerMessage(EventId = 31006, Level = LogLevel.Information, Message = "\tMount (database file):      {hostPath} => {containerPath}")]
        public static partial void DockerMountDatabase(this ILogger logger, string hostPath, string containerPath);

        [LoggerMessage(EventId = 31007, Level = LogLevel.Information, Message = "\tMount (temporary storage):  {hostPath} => {containerPath}")]
        public static partial void DockerMountTempDirectory(this ILogger logger, string hostPath, string containerPath);

        [LoggerMessage(EventId = 31008, Level = LogLevel.Information, Message = "\tMount (application logs):   {hostPath} => {containerPath}")]
        public static partial void DockerMountAppLogs(this ILogger logger, string hostPath, string containerPath);

        [LoggerMessage(EventId = 31009, Level = LogLevel.Debug, Message = "{applicationName} created with container ID {id}.")]
        public static partial void DockerContainerCreated(this ILogger logger, string applicationName, string id);

        [LoggerMessage(EventId = 31010, Level = LogLevel.Debug, Message = "Starting container {id}...")]
        public static partial void DockerStartContainer(this ILogger logger, string id);

        [LoggerMessage(EventId = 31011, Level = LogLevel.Error, Message = "Error starting container {id}.")]
        public static partial void DockerContainerStartError(this ILogger logger, string id);

        [LoggerMessage(EventId = 31012, Level = LogLevel.Information, Message = "{applicationName} started with container ID {id}")]
        public static partial void DockerContainerStarted(this ILogger logger, string applicationName, string id);

        [LoggerMessage(EventId = 31013, Level = LogLevel.Debug, Message = "Stopping {applicationName} with container ID {id}.")]
        public static partial void DockerContainerStopping(this ILogger logger, string applicationName, string id);

        [LoggerMessage(EventId = 31014, Level = LogLevel.Debug, Message = "{applicationName} with container ID {id} stopped.")]
        public static partial void DockerContainerStopped(this ILogger logger, string applicationName, string id);

        [LoggerMessage(EventId = 31015, Level = LogLevel.Warning, Message = "Warnings: {warnings}")]
        public static partial void DockerCreateWarnings(this ILogger logger, string warnings);

        [LoggerMessage(EventId = 31016, Level = LogLevel.Information, Message = "\tMount (plug-ins):   {hostPath} => {containerPath}")]
        public static partial void DockerMountPlugins(this ILogger logger, string hostPath, string containerPath);
    }
}
