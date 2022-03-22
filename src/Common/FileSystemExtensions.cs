// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO.Abstractions;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public static class FileSystemExtensions
    {
        public static void CreateDirectoryIfNotExists(this IDirectory directory, string path)
        {
            Guard.Against.NullOrWhiteSpace(path, nameof(path));
            if (!directory.Exists(path))
            {
                directory.CreateDirectory(path);
            }
        }

        public static bool TryDelete(this IDirectory directory, string dirPath)
        {
            Guard.Against.NullOrWhiteSpace(dirPath, nameof(dirPath));
            try
            {
                directory.Delete(dirPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGenerateDirectory(this IDirectory directory, string path, out string generatedPath)
        {
            Guard.Against.NullOrWhiteSpace(path, nameof(path));

            var tryCount = 0;
            do
            {
                generatedPath = $"{path}-{DateTime.UtcNow.Millisecond}";
                try
                {
                    directory.CreateDirectory(generatedPath);
                    return true;
                }
                catch
                {
                    if (++tryCount > 5)
                    {
                        return false;
                    }
                }
            } while (true);
        }
    }
}
