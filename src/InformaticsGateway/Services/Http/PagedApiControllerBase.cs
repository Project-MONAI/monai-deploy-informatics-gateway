using System;
using System.Collections.Generic;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Common.Pagination;
using Monai.Deploy.InformaticsGateway.Services.UriService;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    public class PagedApiControllerBase : ApiControllerBase
    {
        protected readonly IOptions<InformaticsGatewayConfiguration> EndpointOptions;

        public PagedApiControllerBase(IOptions<InformaticsGatewayConfiguration> options)
        {
            EndpointOptions = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Creates a pagination paged response.
        /// </summary>
        /// <typeparam name="T">Data set type.</typeparam>
        /// <param name="pagedData">Data set.</param>
        /// <param name="validFilter">Filters.</param>
        /// <param name="totalRecords">Total records.</param>
        /// <param name="uriService">Uri service.</param>
        /// <param name="route">Route.</param>
        /// <returns>Returns <see cref="PagedResponse{T}"/>.</returns>
        public PagedResponse<IEnumerable<T>> CreatePagedResponse<T>(IEnumerable<T> pagedData, PaginationFilter validFilter, long totalRecords, IUriService uriService, string route)
        {
            Guard.Against.Null(pagedData, nameof(pagedData));
            Guard.Against.Null(validFilter, nameof(validFilter));
            Guard.Against.Null(route, nameof(route));
            Guard.Against.Null(uriService, nameof(uriService));

            var pageSize = validFilter.PageSize ?? EndpointOptions.Value.EndpointSettings.DefaultPageSize;
            var response = new PagedResponse<IEnumerable<T>>(pagedData, validFilter.PageNumber ?? 0, pageSize);

            response.SetUp(validFilter, totalRecords, uriService, route);
            return response;
        }
    }
}
