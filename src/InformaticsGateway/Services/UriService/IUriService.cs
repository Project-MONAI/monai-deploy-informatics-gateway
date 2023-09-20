using Monai.Deploy.InformaticsGateway.Services.Common.Pagination;

namespace Monai.Deploy.InformaticsGateway.Services.UriService
{
    /// <summary>
    /// Uri Service.
    /// </summary>
    public interface IUriService
    {
        /// <summary>
        /// Gets Relative Uri path with filters as a string.
        /// </summary>
        /// <param name="filter">Filters.</param>
        /// <param name="route">Route.</param>
        /// <returns>Relative Uri string.</returns>
        public string GetPageUriString(PaginationFilter filter, string route);
    }
}
