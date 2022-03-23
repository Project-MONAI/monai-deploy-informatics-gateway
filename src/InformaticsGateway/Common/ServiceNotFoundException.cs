// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.Common
{
    [Serializable]
    public class ServiceNotFoundException : Exception
    {
        private static readonly string MessageFormat = "Required service '{0}' cannot be found or cannot be initialized.";

        public ServiceNotFoundException(string serviceName)
            : base(string.Format(CultureInfo.InvariantCulture, MessageFormat, serviceName))
        {
        }

        public ServiceNotFoundException(string serviceName, Exception innerException)
            : base(string.Format(CultureInfo.InvariantCulture, MessageFormat, serviceName), innerException)
        {
        }

        private ServiceNotFoundException()
        {
        }

        protected ServiceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
