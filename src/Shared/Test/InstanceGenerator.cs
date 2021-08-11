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

using Dicom;
using Dicom.Network;
using System.IO;
using System.IO.Abstractions;

namespace Monai.Deploy.InformaticsGateway.Shared.Test
{
    public class InstanceGenerator
    {
        public static DicomCStoreRequest GenerateDicomCStoreRequest()
        {
            return new DicomCStoreRequest(GenerateDicomFile());
        }

        public static DicomFile GenerateDicomFile(
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            string sopInstanceUid = null,
            IFileSystem fileSystem = null)
        {
            var dataset = GenerateDicomDataset(studyInstanceUid, seriesInstanceUid, ref sopInstanceUid);

            fileSystem?.File.Create($"{sopInstanceUid}.dcm");
            return new DicomFile(dataset);
        }

        private static DicomDataset GenerateDicomDataset(string studyInstanceUid, string seriesInstanceUid, ref string sopInstanceUid)
        {
            if (string.IsNullOrWhiteSpace(sopInstanceUid))
            {
                sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            }
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.PatientID, "PID");
            dataset.Add(DicomTag.StudyInstanceUID, studyInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID().UID);
            dataset.Add(DicomTag.SeriesInstanceUID, seriesInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID().UID);
            dataset.Add(DicomTag.SOPInstanceUID, sopInstanceUid);
            dataset.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID);
            return dataset;
        }

        public static byte[] GenerateDicomData(
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            string sopInstanceUid = null)
        {
            var dataset = GenerateDicomDataset(studyInstanceUid, seriesInstanceUid, ref sopInstanceUid);

            var dicomfile = new DicomFile(dataset);
            using var ms = new MemoryStream();
            dicomfile.Save(ms);
            return ms.ToArray();
        }
    }
}