// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0


using System;

namespace Monai.Deploy.InformaticsGateway.Client.Common
{
    [Serializable]
    public class ProblemDetails
    {
        public string Title { get; set; }
        public int Status { get; set; }
        public string Detail { get; set; }
    }
}
