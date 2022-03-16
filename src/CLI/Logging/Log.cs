// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.CLI.Services;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public static partial class Log
    {

        [LoggerMessage(EventId = 30000, Level = LogLevel.Critical, Message = "{message}")]
        public static partial void CriticalException(this ILogger logger, string message);

        [LoggerMessage(EventId = 30001, Level = LogLevel.Debug, Message = "{messge}")]
        public static partial void DebugMessage(this ILogger logger, string messge);

        [LoggerMessage(EventId = 30002, Level = LogLevel.Critical, Message = "{message}")]
        public static partial void ConfigurationException(this ILogger logger, string message);

        [LoggerMessage(EventId = 30003, Level = LogLevel.Information, Message = "New MONAI Deploy SCP Application Entity created:\r\n\tName:     {name}\r\n\tAE Title:     {aeTitle}\r\n\tGrouping:     {grouping}\r\n\tTimeout:     {timeout}")]
        public static partial void MonaiAeTitleCreated(this ILogger logger, string name, string aeTitle, string grouping, uint timeout);

        [LoggerMessage(EventId = 30004, Level = LogLevel.Information, Message = "\tWorkflows: {workflows}")]
        public static partial void MonaiAeWorkflows(this ILogger logger, string workflows);

        [LoggerMessage(EventId = 30005, Level = LogLevel.Warning, Message = "Data received by this Application Entity will bypass Data Routing Service.")]
        public static partial void WorkflowWarning(this ILogger logger);

        [LoggerMessage(EventId = 30006, Level = LogLevel.Information, Message = "\tIgnored SOP Classes: {ignoredSopClasses}")]
        public static partial void MonaiAeIgnoredSops(this ILogger logger, string ignoredSopClasses);

        [LoggerMessage(EventId = 30007, Level = LogLevel.Warning, Message = "Instances with matching SOP class UIDs are accepted but dropped.")]
        public static partial void IgnoreSopClassesWarning(this ILogger logger);

        [LoggerMessage(EventId = 30008, Level = LogLevel.Critical, Message = "Error creating MONAI SCP AE Title {aeTitle}: {message}.")]
        public static partial void MonaiAeCreateCritical(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30009, Level = LogLevel.Critical, Message = "Error retrieving MONAI SCP AE Titles: {message}.")]
        public static partial void ErrorListingMonaiAeTitles(this ILogger logger, string message);

        [LoggerMessage(EventId = 30010, Level = LogLevel.Warning, Message = "No MONAI SCP Application Entities configured.")]
        public static partial void NoAeTitlesFound(this ILogger logger);

        [LoggerMessage(EventId = 30011, Level = LogLevel.Information, Message = "MONAI SCP AE Title '{aeTitle}' deleted.")]
        public static partial void MonaiAeTitleDeleted(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 30012, Level = LogLevel.Critical, Message = "Error deleting MONAI SCP AE Title {aeTitle}: {message}")]
        public static partial void ErrorDeletingMonaiAeTitle(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30013, Level = LogLevel.Information, Message = "Informatics Gateway API: {endpoint}")]
        public static partial void ConfigInformaticsGatewayApiEndpoint(this ILogger logger, string endpoint);

        [LoggerMessage(EventId = 30014, Level = LogLevel.Information, Message = "DICOM SCP Listening Port: {port}")]
        public static partial void ConfigDicomScpPort(this ILogger logger, int port);

        [LoggerMessage(EventId = 30015, Level = LogLevel.Information, Message = "Container Runner: {runner}")]
        public static partial void ConfigContainerRunner(this ILogger logger, Runner runner);

        [LoggerMessage(EventId = 30016, Level = LogLevel.Information, Message = "Host:\r\n\tDatabase storage mount: {database}\r\n\tData storage mount: {data}\r\n\tLogs storage mount: {log}")]
        public static partial void ConfigHostInfo(this ILogger logger, string database, string data, string log);

        [LoggerMessage(EventId = 30017, Level = LogLevel.Information, Message = "New DICOM destination created:\r\n\tName:     {name}\r\n\tAE Title:     {aeTitle}\r\n\tHost/IP Address:     {hostIp}\r\n\tPort:     {port}")]
        public static partial void DicomDestinationCreated(this ILogger logger, string name, string aeTitle, string hostIp, int port);

        [LoggerMessage(EventId = 30018, Level = LogLevel.Critical, Message = "Error creating DICOM destination {aeTitle}: {message}")]
        public static partial void ErrorCreatingDicomDestination(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30019, Level = LogLevel.Information, Message = "DICOM destination '{name}' deleted.")]
        public static partial void DicomDestinationDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 30020, Level = LogLevel.Critical, Message = "Error deleting DICOM destination {aeTitle}: {message}")]
        public static partial void ErrorDeletingDicomDestination(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30021, Level = LogLevel.Critical, Message = "Error retrieving DICOM destinations: {message}.")]
        public static partial void ErrorListingDicomDestinations(this ILogger logger, string message);

        [LoggerMessage(EventId = 30022, Level = LogLevel.Warning, Message = "No DICOM destinations configured.")]
        public static partial void NoDicomDestinationFound(this ILogger logger);


        [LoggerMessage(EventId = 30023, Level = LogLevel.Critical, Message = "Error creating DICOM source {aeTitle}: {message}")]
        public static partial void ErrorCreatingDicomSource(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30024, Level = LogLevel.Information, Message = "DICOM source '{name}' deleted.")]
        public static partial void DicomSourceDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 30025, Level = LogLevel.Critical, Message = "Error deleting DICOM source {aeTitle}: {message}")]
        public static partial void ErrorDeletingDicomSource(this ILogger logger, string aeTitle, string message);

        [LoggerMessage(EventId = 30026, Level = LogLevel.Critical, Message = "Error retrieving DICOM sources: {message}.")]
        public static partial void ErrorListingDicomSources(this ILogger logger, string message);

        [LoggerMessage(EventId = 30027, Level = LogLevel.Warning, Message = "No DICOM sources configured.")]
        public static partial void NoDicomSourcesFound(this ILogger logger);

        [LoggerMessage(EventId = 30028, Level = LogLevel.Information, Message = "New DICOM source created:\r\n\tName:     {name}\r\n\tAE Title:     {aeTitle}\r\n\tHost/IP Address:     {hostIp}")]
        public static partial void DicomSourceCreated(this ILogger logger, string name, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 30029, Level = LogLevel.Critical, Message = "Error retrieving service status: {message}")]
        public static partial void ErrorRetrievingStatus(this ILogger logger, string message);

        [LoggerMessage(EventId = 30030, Level = LogLevel.Information, Message = "Number of active DIMSE connections: {connections}")]
        public static partial void StatusDimseConnections(this ILogger logger, int connections);

        [LoggerMessage(EventId = 30031, Level = LogLevel.Information, Message = "Service Status:")]
        public static partial void ServiceStatusHeader(this ILogger logger);

        [LoggerMessage(EventId = 30032, Level = LogLevel.Information, Message = "\t\t{name}: {status}")]
        public static partial void ServiceStatusItem(this ILogger logger, string name, ServiceStatus status);


    }
}
