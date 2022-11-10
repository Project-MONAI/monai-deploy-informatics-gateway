/*
 * Copyright 2022 MONAI Consortium
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

        public DataProvider(Configurations configurations, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _outputHelper.WriteLine("DicomDataProvider {0}", Guid.NewGuid());

            _dicomInstanceGenerator = new DicomInstanceGenerator(outputHelper);
        }

        internal void GenerateDicomData(string modality, int studyCount, int? seriesPerStudy = null)
        {
            Guard.Against.NullOrWhiteSpace(modality);

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

        internal void ReplaceGeneratedDicomDataWithHashes()
        {
            var dicomFileSize = new Dictionary<string, string>();
            foreach (var dicomFile in DicomSpecs.Files)
            {
                var key = dicomFile.GenerateFileName();
                dicomFileSize[key] = dicomFile.CalculateHash();
            }

            DicomSpecs.FileHashes = dicomFileSize;
            DicomSpecs.Files.Clear();
        }

        internal void GenerateAcrRequest(string requestType)
        {
            Guard.Against.NullOrWhiteSpace(requestType);

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
                    inferenceRequest.InputMetadata.Details.PatientId = DicomSpecs.Files[0].Dataset.GetSingleValue<string>(DicomTag.PatientID);
                    break;

                case "AccessionNumber":
                    inferenceRequest.InputMetadata.Details.Type = InferenceRequestType.AccessionNumber;
                    inferenceRequest.InputMetadata.Details.AccessionNumber = new List<string>() { DicomSpecs.Files[0].Dataset.GetSingleValue<string>(DicomTag.AccessionNumber) };
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
