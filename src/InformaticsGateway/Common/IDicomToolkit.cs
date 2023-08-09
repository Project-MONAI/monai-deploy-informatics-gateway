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

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public interface IDicomToolkit
    {
        Task<DicomFile> OpenAsync(Stream stream, FileReadOption fileReadOption = FileReadOption.Default);

        DicomFile Load(byte[] fileContent);

        StudySerieSopUids GetStudySeriesSopInstanceUids(DicomFile dicomFile);

        static DicomTag GetDicomTagByName(string tag) => DicomDictionary.Default[tag] ?? DicomDictionary.Default[Regex.Replace(tag, @"\s+", "", RegexOptions.None, TimeSpan.FromSeconds(1))];
    }
}
