// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

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
