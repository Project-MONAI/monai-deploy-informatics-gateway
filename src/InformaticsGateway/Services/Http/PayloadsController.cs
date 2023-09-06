using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common.Filter;
using Monai.Deploy.InformaticsGateway.Common.Pagination;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    /// <summary>
    /// Payloads Controller.
    /// </summary>
    [Route("payload")]
    public class PayloadsController : ApiControllerBase
    {
        private readonly ILogger<PayloadsController> _logger;
        private readonly IUriService _uriService;               // TODO DI
        private readonly IPayloadService _payloadService;       //TODO DI

        /// <summary>
        /// Initializes a new instance of the <see cref="PayloadsController"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="uriService">Uri Service.</param>
        /// <param name="payloadService">payload service to retrieve payloads.</param>
        /// <param name="options">Http options</param>
        public PayloadsController(ILogger<PayloadsController> logger,
            IUriService uriService, IPayloadService payloadService,
            IOptions<HttpPaginationConfiguration> options) : base(options)
        {
            _logger = logger;
            _uriService = uriService;
            _payloadService = payloadService;
        }

        /// <summary>
        /// Gets a paged response list of all payloads.
        /// </summary>
        /// <param name="filter">Filters.</param>
        /// <param name="patientId">Optional patient Id.</param>
        /// <param name="patientName">Optional patient name.</param>
        /// <returns>paged response of subset of all workflows.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<List<Payload>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAsync([FromQuery] PaginationFilter filter,
            [FromQuery] string patientId = "", [FromQuery] string patientName = "")
        {
            try
            {
                var route = Request?.Path.Value ?? string.Empty;
                var pageSize = filter.PageSize ?? Options.Value.DefaultPageSize;
                var validFilter = new PaginationFilter(filter.PageNumber, pageSize, Options.Value.MaxPageSize);

                var pagedData = await _payloadService.GetAllAsync(
                    (validFilter.PageNumber - 1) * validFilter.PageSize,
                    validFilter.PageSize,
                    patientId,
                    patientName).ConfigureAwait(false);

                var dataTotal = await _payloadService.CountAsync().ConfigureAwait(false);
                var pagedReponse = CreatePagedResponse(pagedData.ToList(), validFilter, dataTotal, _uriService, route);

                return Ok(pagedReponse);
            }
            catch (Exception e)
            {
                _logger.PayloadGetAllAsyncError(e);
                return Problem($"Unexpected error occurred: {e.Message}", $"/payload", InternalServerError);
            }
        }
    }

    public interface IPayloadService : IPaginatedApi<Payload>
    {
        /// <summary>
        /// Gets a list of payloads.
        /// </summary>
        Task<IList<Payload>> GetAllAsync(int? skip,
            int? limit,
            string patientId,
            string patientName);
    }

    public interface IPaginatedApi<T>
    {
        Task<long> CountAsync();

        Task<IList<T>> GetAllAsync(int? skip, int? limit);
    }

    class PayloadService : IPayloadService
    {
        private readonly IPayloadRepository _payloadRepository;

        public PayloadService(IPayloadRepository payloadRepository)
        {
            _payloadRepository = payloadRepository;
        }
        public Task<long> CountAsync() => _payloadRepository.CountAsync();

        public Task<IList<Payload>> GetAllAsync(int? skip = null, int? limit = null) =>
            GetAllAsync(skip, limit, string.Empty, string.Empty);

        public Task<IList<Payload>> GetAllAsync(int? skip,
            int? limit,
            string patientId,
            string patientName)
            => _payloadRepository.GetAllAsync(skip, limit, patientId, patientName);
    }
}
