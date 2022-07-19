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

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 50, Level = LogLevel.Debug, Message = "Attempting to save file to {filePath}.")]
        public static partial void SavingFile(this ILogger logger, string filePath);

        [LoggerMessage(EventId = 51, Level = LogLevel.Information, Message = "File saved as {filename}.")]
        public static partial void FileSaved(this ILogger logger, string filename);

        [LoggerMessage(EventId = 52, Level = LogLevel.Debug, Message = "Not a valid DICOM part-10 file {filename}, skipping.")]
        public static partial void SkippingNoneDicomFiles(this ILogger logger, string filename);

        [LoggerMessage(EventId = 53, Level = LogLevel.Debug, Message = "Attempting to save file to {filePath} and {metadataFilePath}.")]
        public static partial void SavingDicomFile(this ILogger logger, string filePath, string metadataFilePath);

        [LoggerMessage(EventId = 54, Level = LogLevel.Debug, Message = "Unable to restore file {filename}.")]
        public static partial void UnableToRestoreFile(this ILogger logger, string filename);

        [LoggerMessage(EventId = 55, Level = LogLevel.Warning, Message = "Directory `{path}` does not exist; no files restored.")]
        public static partial void DirectoryDoesNotExistsNoFilesRestored(this ILogger logger, string path);
    }
}
