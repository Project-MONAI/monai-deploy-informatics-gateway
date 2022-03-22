// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    public interface IServiceBase
    {
        bool TryConfigureServiceUriPrefix(string uriPrefix);
    }
}
