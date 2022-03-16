// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.IO;
using System.IO.Abstractions;
using FellowOakDicom;
using FellowOakDicom.Network;

namespace Monai.Deploy.InformaticsGateway.SharedTest
{
    public static class InstanceGenerator
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
            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, studyInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID().UID },
                { DicomTag.SeriesInstanceUID, seriesInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID().UID },
                { DicomTag.SOPInstanceUID, sopInstanceUid },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
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
