using System;
using Monai.Deploy.InformaticsGateway.Services.UriService;

namespace Monai.Deploy.InformaticsGateway.Services.Common.Pagination
{
    /// <summary>
    /// Paged Response for use with pagination's.
    /// </summary>
    /// <typeparam name="T">Type of response.</typeparam>
    public class PagedResponse<T> : Response<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResponse{T}"/> class.
        /// </summary>
        /// <param name="data">Response Data.</param>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="pageSize">Page size.</param>
        public PagedResponse(T data, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            Data = data;
            Message = null;
            Succeeded = true;
            Errors = null;
        }

        /// <summary>
        /// Gets or sets PageNumber.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets PageSize.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets FirstPage.
        /// </summary>
        public string? FirstPage { get; set; }

        /// <summary>
        /// Gets or sets LastPage.
        /// </summary>
        public string? LastPage { get; set; }

        /// <summary>
        /// Gets or sets TotalPages.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets TotalRecords.
        /// </summary>
        public long TotalRecords { get; set; }

        /// <summary>
        /// Gets or sets NextPage.
        /// </summary>
        public string? NextPage { get; set; }

        /// <summary>
        /// Gets or sets previousPage.
        /// </summary>
        public string? PreviousPage { get; set; }

        public void SetUp(PaginationFilter validFilter, long totalRecords, IUriService uriService, string route)
        {
            var totalPages = (double)totalRecords / PageSize;
            var roundedTotalPages = Convert.ToInt32(Math.Ceiling(totalPages));

            var pageNumber = validFilter.PageNumber ?? 0;
            NextPage =
                pageNumber >= 1 && pageNumber < roundedTotalPages
                ? uriService.GetPageUriString(new PaginationFilter(pageNumber + 1, PageSize), route)
                : null;

            PreviousPage =
                pageNumber - 1 >= 1 && pageNumber <= roundedTotalPages
                ? uriService.GetPageUriString(new PaginationFilter(pageNumber - 1, PageSize), route)
                : null;

            FirstPage = uriService.GetPageUriString(new PaginationFilter(1, PageSize), route);
            LastPage = uriService.GetPageUriString(new PaginationFilter(roundedTotalPages, PageSize), route);
            TotalPages = roundedTotalPages;
            TotalRecords = totalRecords;
        }
    }
}
