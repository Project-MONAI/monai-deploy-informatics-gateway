/*
 * Copyright 2021-2022 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
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
