/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2020 NVIDIA Corporation
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

using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public interface IDicomToolkit
    {
        DicomFile Open(string path, FileReadOption fileReadOption = FileReadOption.Default);

        Task<DicomFile> OpenAsync(Stream stream, FileReadOption fileReadOption = FileReadOption.Default);

        bool HasValidHeader(string path);

        bool TryGetString(DicomFile file, DicomTag dicomTag, out string value);

        Task Save(DicomFile file, string filename, string metadataFilename, DicomJsonOptions dicomJsonOptions);

        DicomFile Load(byte[] fileContent);

        StudySerieSopUids GetStudySeriesSopInstanceUids(string dicomFilePath);

        StudySerieSopUids GetStudySeriesSopInstanceUids(DicomFile dicomFile);
    }
}
