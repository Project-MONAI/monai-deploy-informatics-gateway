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

using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class DicomDataSpecs
    {
        public List<string> StudyInstanceUids { get; set; }
        public int StudyCount { get; set; }
        public int SeriesPerStudyCount { get; set; }
        public int InstancePerSeries { get; set; }
        public int FileCount { get; set; }
        public List<DicomFile> Files { get; set; }
        public Dictionary<string, string> FileHashes { get; set; } = new Dictionary<string, string>();

        public int NumberOfExpectedRequests(string grouping) => grouping switch
        {
            "0020,000D" => StudyCount,
            "0020,000E" => StudyCount * SeriesPerStudyCount,
            "stow_none" => 1, // For DICOMweb STOW-RS
            "stow_study" => 1, // For DICOMweb STOW-RS
            _ => throw new ArgumentException($"Grouping '{grouping} not supported.")
        };

        public int NumberOfExpectedFiles(string grouping) => grouping switch
        {
            "0020,000D" => SeriesPerStudyCount * InstancePerSeries,
            "0020,000E" => InstancePerSeries,
            "stow_none" => FileCount, // For DICOMweb STOW-RS
            "stow_study" => SeriesPerStudyCount * InstancePerSeries, // For DICOMweb STOW-RS
            _ => throw new ArgumentException($"Grouping '{grouping} not supported.")
        };
    }
}
