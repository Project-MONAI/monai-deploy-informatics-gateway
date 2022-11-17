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
using Ardalis.GuardClauses;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public class DicomToolkit : IDicomToolkit
    {
        public Task<DicomFile> OpenAsync(Stream stream, FileReadOption fileReadOption = FileReadOption.Default)
        {
            Guard.Against.Null(stream);

            return DicomFile.OpenAsync(stream, fileReadOption);
        }

        public DicomFile Load(byte[] fileContent)
        {
            Guard.Against.NullOrEmpty(fileContent);

            using var stream = new MemoryStream(fileContent);
            var dicomFile = DicomFile.Open(stream, FileReadOption.ReadAll);

            if (dicomFile is null)
            {
                throw new DicomDataException("Invalid DICOM content");
            }
            return dicomFile;
        }

        public StudySerieSopUids GetStudySeriesSopInstanceUids(DicomFile dicomFile)
        {
            Guard.Against.Null(dicomFile);

            return new StudySerieSopUids
            {
                SopClassUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID),
                StudyInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID),
                SeriesInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID),
                SopInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
            };
        }
    }
}
