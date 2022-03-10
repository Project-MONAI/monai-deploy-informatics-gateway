// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.IO;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface IEmbeddedResource
    {
        Stream GetManifestResourceStream(string name);
    }

    public class EmbeddedResource : IEmbeddedResource
    {
        public Stream GetManifestResourceStream(string name)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));
            return GetType().Assembly.GetManifestResourceStream(Common.AppSettingsResourceName);
        }
    }
}
