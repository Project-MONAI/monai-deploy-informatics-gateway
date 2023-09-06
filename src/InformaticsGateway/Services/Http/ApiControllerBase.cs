using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Common.Filter;
using Monai.Deploy.InformaticsGateway.Common.Pagination;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IOptions<HttpPaginationConfiguration> Options { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiControllerBase"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        protected ApiControllerBase(IOptions<HttpPaginationConfiguration> options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets internal Server Error 500.
        /// </summary>
        protected static int InternalServerError => (int)HttpStatusCode.InternalServerError;

        /// <summary>
        /// Gets bad Request 400.
        /// </summary>
        protected static new int BadRequest => (int)HttpStatusCode.BadRequest;

        /// <summary>
        /// Gets notFound 404.
        /// </summary>
        protected static new int NotFound => (int)HttpStatusCode.NotFound;

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
        protected PagedResponse<IEnumerable<T>> CreatePagedResponse<T>(IEnumerable<T> pagedData,
            PaginationFilter validFilter,
            long totalRecords,
            IUriService uriService,
            string route)
        {
            var data = pagedData as T[] ?? pagedData.ToArray();
            Guard.Against.Null(validFilter, nameof(validFilter));
            Guard.Against.Null(route, nameof(route));
            Guard.Against.Null(uriService, nameof(uriService));

            var pageSize = validFilter.PageSize ?? Options.Value.DefaultPageSize;
            var response = new PagedResponse<IEnumerable<T>>(data, validFilter.PageNumber, pageSize);

            response.SetUp(validFilter, totalRecords, uriService, route);
            return response;
        }
    }
}
