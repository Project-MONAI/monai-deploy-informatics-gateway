using System;
using System.Collections.Generic;
using System.Globalization;
using FellowOakDicom;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    public class PatientDetails
    {
        [JsonProperty(PropertyName = "patient_id")]
        public string? PatientId { get; set; }

        [JsonProperty(PropertyName = "patient_name")]
        public string? PatientName { get; set; }

        [JsonProperty(PropertyName = "patient_sex")]
        public string? PatientSex { get; set; }

        [JsonProperty(PropertyName = "patient_dob")]
        public DateTime? PatientDob { get; set; }

        [JsonProperty(PropertyName = "patient_age")]
        public string? PatientAge { get; set; }

        [JsonProperty(PropertyName = "patient_hospital_id")]
        public string? PatientHospitalId { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static class Dicom
        {
            private const string PatientIdTag = "00100020";
            private const string PatientNameTag = "00100010";
            private const string PatientSexTag = "00100040";
            private const string PatientDateOfBirthTag = "00100030";
            private const string PatientAgeTag = "00101010";
            private const string PatientHospitalIdTag = "00100021";

            public static PatientDetails Get(DicomDataset dataset)
            {
                var dicomTags = new Dictionary<DicomTag, Action<PatientDetails, string>>
                {
                    { DicomTag.Parse(PatientIdTag), (p, s) => p.PatientId = s },
                    { DicomTag.Parse(PatientNameTag), (p, s) => p.PatientName = s },
                    { DicomTag.Parse(PatientSexTag), (p, s) => p.PatientSex = s },
                    { DicomTag.Parse(PatientAgeTag), (p, s) => p.PatientAge = s },
                    { DicomTag.Parse(PatientHospitalIdTag), (p, s) => p.PatientHospitalId = s },
                    {
                        DicomTag.Parse(PatientDateOfBirthTag), (p,
                            s) =>
                        {
                            if (string.IsNullOrWhiteSpace(s)) return;

                            p.PatientDob = DateTime.ParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture);
                        }
                    },
                };

                var patientDetails = new PatientDetails();
                foreach (var tag in dicomTags)
                {
                    dataset.TryGetSingleValue<string>(tag.Key, out var val);
                    tag.Value(patientDetails, val);
                }

                return patientDetails;
            }
        }
    }
}
