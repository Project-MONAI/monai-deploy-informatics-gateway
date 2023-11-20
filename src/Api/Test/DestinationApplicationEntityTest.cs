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

using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class MonaiApplicationEntityTest
    {
        [Fact]
        public void GivenAMonaiApplicationEntity_WhenNameIsNotSet_ExpectSetDefaultValuesToBeUsed()
        {
            var entity = new MonaiApplicationEntity
            {
                AeTitle = "AET",
            };

            entity.SetDefaultValues();

            Assert.Equal(entity.AeTitle, entity.Name);
            Assert.Equal("0020,000D", entity.Grouping);
            Assert.NotNull(entity.Workflows);
            Assert.NotNull(entity.IgnoredSopClasses);
            Assert.NotNull(entity.AllowedSopClasses);
            Assert.Empty(entity.Workflows);
            Assert.Empty(entity.IgnoredSopClasses);
            Assert.Empty(entity.AllowedSopClasses);
        }

        [Fact]
        public void GivenAMonaiApplicationEntity_WhenNameIsSet_ExpectSetDefaultValuesToNotOverwrite()
        {
            var entity = new MonaiApplicationEntity
            {
                AeTitle = "AET",
                Name = "Name",
                Workflows = new System.Collections.Generic.List<string> { "WORKFLOW" },
                IgnoredSopClasses = new System.Collections.Generic.List<string> { "IgnoredSopClasses" },
                AllowedSopClasses = new System.Collections.Generic.List<string> { "AllowedSopClasses" },
                Grouping = "1234"
            };

            entity.SetDefaultValues();

            Assert.Equal("Name", entity.Name);
            Assert.Equal("1234", entity.Grouping);
            Assert.NotNull(entity.Workflows);
            Assert.NotNull(entity.IgnoredSopClasses);
            Assert.NotNull(entity.AllowedSopClasses);
            Assert.Single(entity.Workflows);
            Assert.Single(entity.IgnoredSopClasses);
            Assert.Single(entity.AllowedSopClasses);
        }
    }
}
