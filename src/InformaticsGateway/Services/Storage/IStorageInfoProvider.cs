// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    public interface IStorageInfoProvider
    {
        bool HasSpaceAvailableToStore { get; }
        bool HasSpaceAvailableForExport { get; }
        bool HasSpaceAvailableToRetrieve { get; }
        long AvailableFreeSpace { get; }
    }
}
