using System;

namespace Monai.Deploy.InformaticsGateway.Services.Common.Pagination
{
    public class TimeFilter : PaginationFilter
    {
        public TimeFilter()
        {
        }

        public TimeFilter(DateTime? startTime,
            DateTime? endTime,
            int pageNumber,
            int pageSize,
            int maxPageSize) : base(pageNumber,
            pageSize,
            maxPageSize)
        {
            if (endTime == default)
            {
                EndTime = DateTime.Now;
            }

            if (startTime == default)
            {
                StartTime = new DateTime(2023, 1, 1);
            }
        }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }
    }
}
