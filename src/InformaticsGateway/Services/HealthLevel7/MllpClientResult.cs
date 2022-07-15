// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using HL7.Dotnetcore;

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal class MllpClientResult
    {
        public IList<Message> Messages { get; }
        public AggregateException AggregateException { get; }

        public MllpClientResult(IList<Message> messages, AggregateException aggregateException)
        {
            Messages = messages;
            AggregateException = aggregateException;
        }

    }
}
