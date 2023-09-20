using System;
using System.Collections.Generic;
using Monai.Deploy.InformaticsGateway.Services.Common.Pagination;
using Monai.Deploy.InformaticsGateway.Services.UriService;
using MongoDB.Bson.Serialization.Serializers;
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
            var pagedResponse = new PagedResponse<List<string>>(data,0, 3);
            var uriService = new UriService(new Uri("https://test.com"));
            pagedResponse.SetUp(filter, 9, uriService, "test");

            Assert.Equal(pagedResponse.FirstPage, "/test?pageNumber=1&pageSize=3");
            Assert.Equal(pagedResponse.LastPage, "/test?pageNumber=3&pageSize=3");
            Assert.Equal(pagedResponse.NextPage, "/test?pageNumber=2&pageSize=3");
            Assert.Null(pagedResponse.PreviousPage);

        }
    }
}
