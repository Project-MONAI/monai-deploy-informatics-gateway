// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.Api
{
    [Serializable]
    public class LoggingDataDictionary<K, V> : Dictionary<K, V>
    {
        public LoggingDataDictionary()
        {
        }

        protected LoggingDataDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string ToString()
        {
            var pairs = this.Select(x => string.Format("{0}={1}", x.Key, x.Value));
            return string.Join(", ", pairs);
        }
    }
}
