/*
 * Copyright 2022-2023 MONAI Consortium
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

using System.Globalization;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Network;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;
using static Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions.FhirDefinitions;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal static class DicomRandomDataProvider
    {
        private static readonly Random Random = new();
        private static readonly string AlphaNumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly string Numeric = "0123456789";

        public static void InjectRandomData(this DicomDataset dataset, DicomTag tag)
        {
            var data = string.Empty;
            switch (tag.DictionaryEntry.ValueRepresentations[0].Code)
            {
                case "IS":
                    data = RandomString(Numeric, 12);
                    dataset.AddOrUpdate(tag, Convert.ToInt32(data));
                    return;

                case "UI":
                    data = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
                    break;

                case "LO":
                case "LT":
                    data = RandomString(AlphaNumeric, 64);
                    break;

                case "AE":
                    data = RandomString(AlphaNumeric, 16);
                    break;

                case "CS":
                    data = RandomString(Numeric, 16);
                    break;

                case "FL":
                    var bufferFl = new byte[4];
                    Random.NextBytes(bufferFl);
                    dataset.AddOrUpdate(tag, BitConverter.ToSingle(bufferFl, 0).ToString());
                    return;

                case "FD":
                    var bufferFd = new byte[8];
                    Random.NextBytes(bufferFd);
                    dataset.AddOrUpdate(tag, BitConverter.ToSingle(bufferFd, 0).ToString());
                    return;

                case "OD":
                    var bufferOd = new byte[8];
                    Random.NextBytes(bufferOd);
                    dataset.AddOrUpdate(tag, BitConverter.ToSingle(bufferOd, 0));
                    return;

                case "OF":
                    var bufferOf = new byte[4];
                    Random.NextBytes(bufferOf);
                    dataset.AddOrUpdate(tag, BitConverter.ToSingle(bufferOf, 0));
                    return;

                case "PN":
                    data = RandomString(AlphaNumeric, 64);
                    break;

                case "DA":
                    data = "20000101";
                    break;

                case "DT":
                    data = "20000101000000";
                    break;

                case "TM":
                    data = "000000";
                    break;

                case "SH":
                    data = RandomString(Numeric, 16);
                    break;

                case "DS":
                case "SL":
                case "UL":
                    data = RandomString(Numeric, 4);
                    break;

                case "SS":
                case "US":
                    data = RandomString(Numeric, 2);
                    break;

                case "OB":
                case "OW":
                    var bufferBytes = new byte[4];
                    Random.NextBytes(bufferBytes);
                    dataset.AddOrUpdate(tag, bufferBytes);
                    break;

                case "ST":
                case "UN":
                case "UT":
                    data = RandomString(AlphaNumeric, 1024);
                    break;

                case "AS":
                    data = $"{RandomString(Numeric, 3).PadLeft(3, '0')}Y";
                    break;
            }
            dataset.AddOrUpdate(tag, data);
        }

        public static string RandomString(string characterSet, int maxLength)
        {
            var length = Random.Next(1, maxLength);
            var output = new char[length];

            for (int i = 0; i < length; i++)
            {
                output[i] = characterSet[Random.Next(characterSet.Length)];
            }

            return new string(output);
        }
    }

    internal class DataProvider
    {
        private readonly Configurations _configurations;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly DicomInstanceGenerator _dicomInstanceGenerator;

        public DicomDataSpecs DicomSpecs { get; private set; }
        public InferenceRequest AcrRequest { get; private set; }
        public FhirMessages FhirSpecs { get; private set; }
        public Hl7Messages HL7Specs { get; private set; }
        public DicomStatus DimseRsponse { get; internal set; }
        public string StudyGrouping { get; internal set; }
        public string[] Workflows { get; internal set; } = null;
        public int ClientTimeout { get; internal set; }
        public int ClientAssociationPulseTime { get; internal set; } = 0;
        public int ClientSendOverAssociations { get; internal set; } = 1;
        public string Source { get; internal set; } = string.Empty;
        public string Destination { get; internal set; } = string.Empty;

        public DataProvider(Configurations configurations, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _outputHelper.WriteLine("DicomDataProvider {0}", Guid.NewGuid());

            _dicomInstanceGenerator = new DicomInstanceGenerator(outputHelper);
        }

        internal void GenerateDicomData(string modality, int studyCount, int? seriesPerStudy = null)
        {
            Guard.Against.NullOrWhiteSpace(modality, nameof(modality));

            _outputHelper.WriteLine($"Generating {studyCount} {modality} study");
            _configurations.StudySpecs.ContainsKey(modality).Should().BeTrue();

            var studySpec = _configurations.StudySpecs[modality];
            var patientId = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            if (seriesPerStudy.HasValue)
            {
                DicomSpecs = _dicomInstanceGenerator.Generate(patientId, studyCount, seriesPerStudy.Value, modality, studySpec);
            }
            else
            {
                DicomSpecs = _dicomInstanceGenerator.Generate(patientId, studyCount, modality, studySpec);
            }
            _outputHelper.WriteLine($"File specs: {DicomSpecs.StudyCount}, {DicomSpecs.SeriesPerStudyCount}, {DicomSpecs.InstancePerSeries}, {DicomSpecs.FileCount}");
        }

        internal void InjectRandomData(params DicomTag[] tags)
        {
            foreach (var dicomFile in DicomSpecs.Files.Values)
            {
                foreach (var tag in tags)
                {
                    dicomFile.Dataset.InjectRandomData(tag);
                }
            }
        }

        internal void ReplaceGeneratedDicomDataWithHashes()
        {
            var dicomFileSize = new Dictionary<string, string>();
            foreach (var dicomFile in DicomSpecs.Files.Values)
            {
                var key = dicomFile.GenerateFileName();
                dicomFileSize[key] = dicomFile.CalculateHash();
            }

            DicomSpecs.FileHashes = dicomFileSize;
            DicomSpecs.Files.Clear();
        }

        internal void GenerateAcrRequest(string requestType)
        {
            Guard.Against.NullOrWhiteSpace(requestType, nameof(requestType));

            var inferenceRequest = new InferenceRequest();
            inferenceRequest.TransactionId = Guid.NewGuid().ToString();
            inferenceRequest.InputMetadata = new InferenceRequestMetadata();
            inferenceRequest.InputMetadata.Details = new InferenceRequestDetails();
            switch (requestType)
            {
                case "Study":
                    inferenceRequest.InputMetadata.Details.Type = InferenceRequestType.DicomUid;
                    inferenceRequest.InputMetadata.Details.Studies = new List<RequestedStudy>();
                    inferenceRequest.InputMetadata.Details.Studies.Add(new RequestedStudy
                    {
                        StudyInstanceUid = DicomSpecs.StudyInstanceUids[0],
                    });
                    break;

                case "Patient":
                    inferenceRequest.InputMetadata.Details.Type = InferenceRequestType.DicomPatientId;
                    inferenceRequest.InputMetadata.Details.PatientId = DicomSpecs.Files.Values.First().Dataset.GetSingleValue<string>(DicomTag.PatientID);
                    break;

                case "AccessionNumber":
                    inferenceRequest.InputMetadata.Details.Type = InferenceRequestType.AccessionNumber;
                    inferenceRequest.InputMetadata.Details.AccessionNumber = new List<string>() { DicomSpecs.Files.Values.First().Dataset.GetSingleValue<string>(DicomTag.AccessionNumber) };
                    break;

                default:
                    throw new ArgumentException($"invalid ACR request type specified in feature file: {requestType}");
            }
            inferenceRequest.InputResources = new List<RequestInputDataResource>
            {
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        Name = "DICOM-RUNNER-TEST",
                        Id = Guid.NewGuid().ToString(),
                    }
                },
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        Uri = _configurations.OrthancOptions.DicomWebRoot,
                        AuthId = _configurations.OrthancOptions.GetBase64EncodedAuthHeader(),
                        AuthType = ConnectionAuthType.Basic
                    }
                }
            };

            if (!inferenceRequest.IsValid(out var details))
            {
                _outputHelper.WriteLine($"Validation error: {details}.");
                throw new Exception(details);
            }

            AcrRequest = inferenceRequest;
        }

        internal async Task GenerateFhirMessages(string version, string format)
        {
            var files = Directory.GetFiles($"data/fhir/{version}", $"*.{format.ToLowerInvariant()}");

            FhirSpecs = new FhirMessages
            {
                FileFormat = format == "XML" ? FileFormat.Xml : FileFormat.Json,
                MediaType = format == "XML" ? "application/fhir+xml" : "application/fhir+json"
            };

            foreach (var file in files)
            {
                _outputHelper.WriteLine($"Adding file {file}");
                FhirSpecs.Files[file] = await File.ReadAllTextAsync(file);
            }
        }

        internal async Task GenerateHl7Messages(string version)
        {
            HL7Specs = new Hl7Messages();
            var files = Directory.GetFiles($"data/hl7/{version}");

            foreach (var file in files)
            {
                var text = await File.ReadAllTextAsync(file);
                var message = new HL7.Dotnetcore.Message(text);
                message.ParseMessage();
                message.SetValue("MSH.10", file);
                HL7Specs.Files[file] = message;
            }
        }
    }
}
