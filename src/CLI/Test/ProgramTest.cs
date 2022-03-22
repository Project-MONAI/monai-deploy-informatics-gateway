// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ProgramTest
    {
        [RetryFact(DisplayName = "Program - runs properly")]
        public void Startup_RunsProperly()
        {
            var host = Program.BuildParser();

            Assert.NotNull(host);
        }
    }
}
