/*
 * Copyright 2022 MONAI Consortium
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

using Monai.Deploy.InformaticsGateway.Api.Models;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class HL7DestinationEntityTest
    {
        [Fact]
        public void GivenAMonaiApplicationEntity_WhenNameIsNotSet_ExepectSetDefaultValuesToBeUsed()
        {
            var entity = new HL7DestinationEntity
            {
                AeTitle = "AET",
            };

            entity.SetDefaultValues();

            Assert.Equal(entity.AeTitle, entity.Name);
        }

        [Fact]
        public void GivenAMonaiApplicationEntity_WhenNameIsSet_ExepectSetDefaultValuesToNotOverwrite()
        {
            var entity = new HL7DestinationEntity
            {
                AeTitle = "AET",
                HostIp = "IP",
                Name = "Name"
            };

            entity.SetDefaultValues();

            Assert.Equal("AET", entity.AeTitle);
            Assert.Equal("IP", entity.HostIp);
            Assert.Equal("Name", entity.Name);
        }
    }
}
