// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    [Serializable]
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message)
        {
        }

        protected ConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
