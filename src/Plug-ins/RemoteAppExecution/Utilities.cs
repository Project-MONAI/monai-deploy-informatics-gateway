/*
 * Copyright 2023 MONAI Consortium
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

using System.Text.RegularExpressions;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution
{
    internal static class Utilities
    {
        private static DicomTag GetDicomTagByName(string tag) => DicomDictionary.Default[tag] ?? DicomDictionary.Default[Regex.Replace(tag, @"\s+", "", RegexOptions.None, TimeSpan.FromSeconds(1))];

        public static DicomTag[] GetTagArrayFromStringArray(string values)
        {
            var names = values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return names.Select(n => GetDicomTagByName(n)).ToArray();
        }

        public static T? GetTagProxyValue<T>(DicomTag tag) where T : class
        {
            // partial implementation for now see
            // https://dicom.nema.org/dicom/2013/output/chtml/part05/sect_6.2.html
            // for full list
            switch (tag.DictionaryEntry.ValueRepresentations[0].Code)
            {
                case "UI":
                case "LO":
                case "LT":
                {
                    return DicomUIDGenerator.GenerateDerivedFromUUID().UID as T;
                }
                case "SH":
                case "AE":
                case "CS":
                case "PN":
                case "ST":
                case "UT":
                {
                    return "no Value" as T;
                }
                default:
                    return default;
            }
        }
    }
}
