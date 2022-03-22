// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api
{
    public class LoggingDataDictionaryTest
    {
        [Fact(DisplayName = "ToString")]
        public void ToStringOverride()
        {
            var input = new LoggingDataDictionary<string, string>
            {
                { "A", "1" },
                { "B", "2" },
                { "C", "3" }
            };

            Assert.Equal("A=1, B=2, C=3", input.ToString());
        }
    }
}
