// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Monai.Deploy.InformaticsGateway.Configuration;

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

        public DicomFile Open(string path, FileReadOption fileReadOption = FileReadOption.Default)
        {
            Guard.Against.NullOrWhiteSpace(path, nameof(path));

            using var stream = _fileSystem.File.OpenRead(path);
            return DicomFile.Open(stream, fileReadOption);
        }

        public Task<DicomFile> OpenAsync(Stream stream, FileReadOption fileReadOption = FileReadOption.Default)
        {
            Guard.Against.Null(stream, nameof(stream));

            return DicomFile.OpenAsync(stream, fileReadOption);
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
            await file.SaveAsync(stream).ConfigureAwait(false);

            if (dicomJsonOptions != DicomJsonOptions.None)
            {
                Guard.Against.NullOrWhiteSpace(metadataFilename, nameof(metadataFilename));

                if (_fileSystem.File.Exists(metadataFilename))
                {
                    _fileSystem.File.Delete(metadataFilename);
                }
                var json = ConvertDicomToJson(file, dicomJsonOptions == DicomJsonOptions.Complete);
                await _fileSystem.File.AppendAllTextAsync(metadataFilename, json).ConfigureAwait(false);
            }
        }

        public DicomFile Load(byte[] fileContent)
        {
            Guard.Against.NullOrEmpty(fileContent, nameof(fileContent));

            using var stream = new MemoryStream(fileContent);
            var dicomFile = DicomFile.Open(stream, FileReadOption.ReadAll);

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

        public StudySerieSopUids GetStudySeriesSopInstanceUids(string dicomFilePath)
        {
            Guard.Against.NullOrWhiteSpace(dicomFilePath, nameof(dicomFilePath));

            var dicomFile = Open(dicomFilePath);

            return GetStudySeriesSopInstanceUids(dicomFile);
        }

        public StudySerieSopUids GetStudySeriesSopInstanceUids(DicomFile dicomFile)
        {
            Guard.Against.Null(dicomFile, nameof(dicomFile));

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
