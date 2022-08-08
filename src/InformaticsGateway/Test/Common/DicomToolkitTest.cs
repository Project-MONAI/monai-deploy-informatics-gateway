/*
 * Copyright 2021-2022 MONAI Consortium
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
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Common;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Common
{
    public class DicomToolkitTest
    {
        private readonly IFileSystem _fileSystem;

        public DicomToolkitTest()
        {
            _fileSystem = new MockFileSystem();
        }

        [Fact(DisplayName = "Load - throws when data is invalid")]
        public void Load_ThrowsWithBadData()
        {
            var random = new Random();
            var bytes = new byte[10];
            random.NextBytes(bytes);

            var dicomToolkit = new DicomToolkit();
            Assert.Throws<DicomFileException>(() => (dicomToolkit.Load(bytes)));
        }

        [Fact(DisplayName = "Load - returns DicomFile")]
        public void Load_ReturnsDicomFile()
        {
            var dicomFile = new DicomFile();
            var expectedSop = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, expectedSop);
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;

            using var memoryStream = new MemoryStream();
            dicomFile.Save(memoryStream);

            var dicomToolkit = new DicomToolkit();
            var result = dicomToolkit.Load(memoryStream.ToArray());

            Assert.Equal(dicomFile.FileMetaInfo.TransferSyntax, result.FileMetaInfo.TransferSyntax);
            Assert.Equal(expectedSop, result.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPInstanceUID));
        }
    }
}
