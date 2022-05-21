// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Common
{
    public record StudySerieSopUids
    {
        public string StudyInstanceUid { get; set; }
        public string SeriesInstanceUid { get; set; }
        public string SopInstanceUid { get; set; }
        public string Identifier { get => $"{StudyInstanceUid}/{SeriesInstanceUid}/{SopInstanceUid}"; }
    }
}
