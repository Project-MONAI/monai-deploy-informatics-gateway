// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2020 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Monai.Deploy.InformaticsGateway.Configuration;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public class DicomToolkit : IDicomToolkit
    {
        private static readonly IList<DicomVR> DicomVrsToIgnore = new List<DicomVR>() { DicomVR.OB, DicomVR.OD, DicomVR.OF, DicomVR.OL, DicomVR.OV, DicomVR.OW, DicomVR.UN };
        private readonly IFileSystem _fileSystem;

        public DicomToolkit(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new System.ArgumentNullException(nameof(fileSystem));
        }

        public bool HasValidHeader(string path)
        {
            Guard.Against.NullOrWhiteSpace(path, nameof(path));
            return DicomFile.HasValidHeader(path);
        }

        public DicomFile Open(string path)
        {
            Guard.Against.NullOrWhiteSpace(path, nameof(path));

            using var stream = _fileSystem.File.OpenRead(path);
            return DicomFile.Open(stream);
        }

        public bool TryGetString(DicomFile file, DicomTag dicomTag, out string value)
        {
            Guard.Against.Null(file, nameof(file));
            Guard.Against.Null(dicomTag, nameof(dicomTag));

            value = string.Empty;
            if (!file.Dataset.Contains(dicomTag))
            {
                return false;
            }

            return file.Dataset.TryGetString(dicomTag, out value);
        }

        public async Task Save(DicomFile file, string filename, string metadataFilename, DicomJsonOptions dicomJsonOptions)
        {
            Guard.Against.Null(file, nameof(file));
            Guard.Against.NullOrWhiteSpace(filename, nameof(filename));

            var directory = _fileSystem.Path.GetDirectoryName(filename);
            _fileSystem.Directory.CreateDirectoryIfNotExists(directory);

            using var stream = _fileSystem.File.Create(filename);
            await file.SaveAsync(stream);

            if (dicomJsonOptions != DicomJsonOptions.None)
            {
                Guard.Against.NullOrWhiteSpace(metadataFilename, nameof(metadataFilename));

                var json = ConvertDicomToJson(file, dicomJsonOptions == DicomJsonOptions.Complete);
                await _fileSystem.File.AppendAllTextAsync(metadataFilename, json);
            }
        }

        public DicomFile Load(byte[] fileContent)
        {
            Guard.Against.NullOrEmpty(fileContent, nameof(fileContent));

            using var stream = new MemoryStream(fileContent);
            var dicomFile = DicomFile.Open(stream);
            if (dicomFile is null)
            {
                throw new DicomDataException("Invalid DICOM content");
            }
            return dicomFile;
        }

        private static string ConvertDicomToJson(DicomFile file, bool writeOtherValueTypes)
        {
            Guard.Against.Null(file, nameof(file));

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DicomJsonConverter(writeTagsAsKeywords: false, autoValidate: true));
            options.WriteIndented = false;

            if (writeOtherValueTypes)
            {
                return JsonSerializer.Serialize(file.Dataset, options);
            }
            else
            {
                var dataset = file.Dataset.Clone();
                dataset.Remove(i => DicomVrsToIgnore.Contains(i.ValueRepresentation));
                return JsonSerializer.Serialize(dataset, options);
            }
        }
    }
}
