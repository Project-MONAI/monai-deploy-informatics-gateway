// Copyright 2021 MONAI Consortium
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
using Dicom;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public class DicomToolkit : IDicomToolkit
    {
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

        public async Task Save(DicomFile file, string path)
        {
            Guard.Against.Null(file, nameof(file));
            Guard.Against.NullOrWhiteSpace(path, nameof(path));

            var directory = _fileSystem.Path.GetDirectoryName(path);
            _fileSystem.Directory.CreateDirectoryIfNotExists(directory);
            using var stream = _fileSystem.File.Create(path);
            await file.SaveAsync(stream);
        }

        public DicomFile Load(byte[] fileContent)
        {
            using var stream = new MemoryStream(fileContent);
            var dicomFile = DicomFile.Open(stream);
            if (dicomFile is null)
            {
                throw new DicomDataException("Invalid DICOM content");
            }
            return dicomFile;
        }
    }
}
