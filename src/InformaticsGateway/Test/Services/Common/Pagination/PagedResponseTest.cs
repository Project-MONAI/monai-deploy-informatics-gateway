/*
 * Copyright 2021-2023 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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

using System;
using System.Collections.Generic;
using Monai.Deploy.InformaticsGateway.Services.Common.Pagination;
using Monai.Deploy.InformaticsGateway.Services.UriService;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Common.Pagination
{
    public class PagedResponseTest
    {
        [Fact]
        public void SetUp_GivenExpectedInput_ReturnsExpectedResult()
        {
            var filter = new PaginationFilter();
            var data = new List<string> { "orange", "apple", "donkey" };
            var pagedResponse = new PagedResponse<List<string>>(data, 0, 3);
            var uriService = new UriService(new Uri("https://test.com"));
            pagedResponse.SetUp(filter, 9, uriService, "test");

            Assert.Equal(pagedResponse.FirstPage, "/test?pageNumber=1&pageSize=3");
            Assert.Equal(pagedResponse.LastPage, "/test?pageNumber=3&pageSize=3");
            Assert.Equal(pagedResponse.NextPage, "/test?pageNumber=2&pageSize=3");
            Assert.Null(pagedResponse.PreviousPage);
        }
    }
}
