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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.Storage.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    public class DicomService : IDicomService
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<DicomService> _logger;

        public DicomService(IStorageService storageService, ILogger<DicomService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static readonly Dictionary<string, string> SupportedTypes = new()
        {
            { "CS", "Code String" },
            { "DA", "Date" },
            { "DS", "Decimal String" },
            { "IS", "Integer String" },
            { "LO", "Long String" },
            { "SH", "Short String" },
            { "UI", "Unique Identifier (UID)" },
            { "UL", "Unsigned Long" },
            { "US", "Unsigned Short" },
        };

        private static readonly Dictionary<string, string> UnsupportedTypes = new()
        {
            { "CS", "Code String" },
            { "DA", "Date" },
            { "DS", "Decimal String" },
            { "IS", "Integer String" },
            { "LO", "Long String" },
            { "SH", "Short String" },
            { "UI", "Unique Identifier (UID)" },
            { "UL", "Unsigned Long" },
            { "US", "Unsigned Short" },
        };

        public async Task<PatientDetails> GetPayloadPatientDetailsAsync(string payloadId, string bucketName)
        {
            Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));
            Guard.Against.NullOrWhiteSpace(payloadId, nameof(payloadId));

            var items = await _storageService.ListObjectsAsync(bucketName, $"{payloadId}/dcm", true);

            var patientDetails = new PatientDetails
            {
                PatientName = await GetFirstValueAsync(items, payloadId, bucketName, DicomTagConstants.PatientNameTag),
                PatientId = await GetFirstValueAsync(items, payloadId, bucketName, DicomTagConstants.PatientIdTag),
                PatientSex = await GetFirstValueAsync(items, payloadId, bucketName, DicomTagConstants.PatientSexTag),
                PatientAge = await GetFirstValueAsync(items, payloadId, bucketName, DicomTagConstants.PatientAgeTag),
                PatientHospitalId = await GetFirstValueAsync(items, payloadId, bucketName, DicomTagConstants.PatientHospitalIdTag)
            };

            var dob = await GetFirstValueAsync(items, payloadId, bucketName, DicomTagConstants.PatientDateOfBirthTag);

            if (DateTime.TryParseExact(dob, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOfBirth))
            {
                patientDetails.PatientDob = dateOfBirth;
            }

            return patientDetails;
        }

        public async Task<string?> GetFirstValueAsync(IList<VirtualFileInfo> items, string payloadId, string bucketId, string keyId)
        {
            Guard.Against.NullOrWhiteSpace(bucketId, nameof(bucketId));
            Guard.Against.NullOrWhiteSpace(payloadId, nameof(payloadId));
            Guard.Against.NullOrWhiteSpace(keyId, nameof(keyId));

            try
            {
                if (items is null || items.Any() is false)
                {
                    return null;
                }

                foreach (var filePath in items.Select(item => item.FilePath))
                {
                    if (filePath.EndsWith(".dcm.json") is false)
                    {
                        continue;
                    }

                    var stream = await _storageService.GetObjectAsync(bucketId, filePath);
                    var jsonStr = Encoding.UTF8.GetString(((MemoryStream)stream).ToArray());

                    var dict = new Dictionary<string, DicomValue>(StringComparer.OrdinalIgnoreCase);
                    JsonConvert.PopulateObject(jsonStr, dict);

                    var value = GetValue(dict, keyId);

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.FailedToGetDicomTagFromPayload(payloadId, keyId, bucketId, e);
            }

            return null;
        }

        public async Task<IEnumerable<string>> GetDicomPathsForTaskAsync(string outputDirectory, string bucketName)
        {
            Guard.Against.NullOrWhiteSpace(outputDirectory, nameof(outputDirectory));
            Guard.Against.NullOrWhiteSpace(bucketName, nameof(bucketName));

            var files = await _storageService.ListObjectsAsync(bucketName, outputDirectory, true);

            var dicomFiles = files?.Where(f => f.FilePath.EndsWith(".dcm"));

            return dicomFiles?.Select(d => d.FilePath)?.ToList() ?? new List<string>();
        }

        public async Task<string> GetAnyValueAsync(string keyId, string payloadId, string bucketId)
        {
            Guard.Against.NullOrWhiteSpace(keyId, nameof(keyId));
            Guard.Against.NullOrWhiteSpace(payloadId, nameof(payloadId));
            Guard.Against.NullOrWhiteSpace(bucketId, nameof(bucketId));

            var path = $"{payloadId}/dcm";
            var listOfFiles = await _storageService.ListObjectsAsync(bucketId, path, true);
            var listOfJsonFiles = listOfFiles.Where(file => file.Filename.EndsWith(".json")).ToList();
            var fileCount = listOfJsonFiles.Count;

            for (int i = 0; i < fileCount; i++)
            {
                var matchValue = await GetDcmJsonFileValueAtIndexAsync(i, path, bucketId, keyId, listOfJsonFiles);

                if (matchValue != null)
                {
                    return matchValue;
                }
            }

            return string.Empty;
        }

        public async Task<string> GetAllValueAsync(string keyId, string payloadId, string bucketId)
        {
            Guard.Against.NullOrWhiteSpace(keyId, nameof(keyId));
            Guard.Against.NullOrWhiteSpace(payloadId, nameof(payloadId));
            Guard.Against.NullOrWhiteSpace(bucketId, nameof(bucketId));

            var path = $"{payloadId}/dcm";
            var listOfFiles = await _storageService.ListObjectsAsync(bucketId, path, true);
            var listOfJsonFiles = listOfFiles.Where(file => file.Filename.EndsWith(".json")).ToList();
            var matchValue = await GetDcmJsonFileValueAtIndexAsync(0, path, bucketId, keyId, listOfJsonFiles);
            var fileCount = listOfJsonFiles.Count;

            for (int i = 0; i < fileCount; i++)
            {
                if (listOfJsonFiles[i].Filename.EndsWith(".dcm"))
                {
                    var currentValue = await GetDcmJsonFileValueAtIndexAsync(i, path, bucketId, keyId, listOfJsonFiles);
                    if (currentValue != matchValue)
                    {
                        return string.Empty;
                    }
                }
            }

            return matchValue;
        }

        /// <summary>
        /// Gets file at position
        /// </summary>
        /// <param name="index"></param>
        /// <param name="path"></param>
        /// <param name="bucketId"></param>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public async Task<string> GetDcmJsonFileValueAtIndexAsync(int index,
                                                              string path,
                                                              string bucketId,
                                                              string keyId,
                                                              List<VirtualFileInfo> items)
        {
            Guard.Against.NullOrWhiteSpace(bucketId, nameof(bucketId));
            Guard.Against.NullOrWhiteSpace(path, nameof(path));
            Guard.Against.NullOrWhiteSpace(keyId, nameof(keyId));
            Guard.Against.Null(items, nameof(items));

            if (index > items.Count)
            {
                return string.Empty;
            }

            var stream = await _storageService.GetObjectAsync(bucketId, items[index].FilePath);
            var jsonStr = Encoding.UTF8.GetString(((MemoryStream)stream).ToArray());

            var dict = new Dictionary<string, DicomValue>(StringComparer.OrdinalIgnoreCase);
            JsonConvert.PopulateObject(jsonStr, dict);
            return GetValue(dict, keyId);
        }

        public string GetValue(Dictionary<string, DicomValue> dict, string keyId)
        {
            if (dict.Any() is false)
            {
                return string.Empty;
            }

            var result = string.Empty;

            if (dict.TryGetValue(keyId, out var value))
            {
                if (string.Equals(keyId, DicomTagConstants.PatientNameTag) || value.Vr.ToUpperInvariant() == "PN")
                {
                    result = GetPatientName(value.Value);
                    _logger.GetPatientName(result);
                    return result;
                }
                var jsonString = DecodeComplexString(value);
                if (SupportedTypes.TryGetValue(value.Vr.ToUpperInvariant(), out var vrFullString))
                {
                    result = TryGetValueAndLogSupported(vrFullString, value, jsonString);
                }
                else if (UnsupportedTypes.TryGetValue(value.Vr.ToUpperInvariant(), out vrFullString))
                {
                    result = TryGetValueAndLogSupported(vrFullString, value, jsonString);
                }
                else
                {
                    result = TryGetValueAndLogUnSupported("Unknown Dicom Type", value, jsonString);
                }
            }
            return result;
        }

        private string TryGetValueAndLogSupported(string vrFullString, DicomValue value, string jsonString)
        {
            var result = TryGetValue(value);
            _logger.SupportedType(value.Vr, vrFullString, jsonString, result);
            return result;
        }

        private string TryGetValueAndLogUnSupported(string vrFullString, DicomValue value, string jsonString)
        {
            var result = TryGetValue(value);
            _logger.UnsupportedType(value.Vr, vrFullString, jsonString, result);
            return result;
        }

        private string TryGetValue(DicomValue value)
        {
            var result = string.Empty;
            foreach (var val in value.Value)
            {
                try
                {
                    if (double.TryParse(val.ToString(), out var dbl))
                    {
                        result = ConcatResult(result, dbl);
                    }
                    else
                    {
                        result = ConcatResult(result, val);
                    }
                }
                catch (Exception ex)
                {
                    _logger.UnableToCastDicomValueToString(DecodeComplexString(value), ex);
                }
            }
            if (value.Value.Length > 1)
            {
                return $"[{result}]";
            }
            return result;
        }

        private static string ConcatResult(string result, dynamic str)
        {
            if (string.IsNullOrWhiteSpace(result))
            {
                result = string.Concat(result, $"{str}");
            }
            else
            {
                result = string.Concat(result, $", {str}");
            }

            return result;
        }

        private static string DecodeComplexString(DicomValue dicomValue)
        {
            return JsonConvert.SerializeObject(dicomValue.Value);
        }

        private static string GetPatientName(object[] values)
        {

            var resultStr = new List<string>();

            foreach (var value in values)
            {
                var valueStr = JObject.FromObject(value)?
                    .GetValue("Alphabetic", StringComparison.OrdinalIgnoreCase)?
                    .Value<string>();

                if (valueStr is not null)
                {
                    resultStr.Add(valueStr);
                }
            }

            if (resultStr.Any() is true)
            {
                return string.Concat(resultStr);
            }

            return string.Empty;
        }
    }
}
