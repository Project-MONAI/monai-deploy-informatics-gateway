// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.Services.Http.DicomWeb
{
    public class StowResult
    {
        public int StatusCode { get; internal set; }
        public DicomDataset Data { get; internal set; }
    }
}
