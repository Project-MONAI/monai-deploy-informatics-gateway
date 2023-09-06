/*
 * Copyright 2023 MONAI Consortium
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

namespace Monai.Deploy.InformaticsGateway.Common.Filter
{
    /// <summary>
    /// Pagination Filter class.
    /// </summary>
    public class PaginationFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaginationFilter"/> class.
        /// </summary>
        public PaginationFilter()
        {
            PageNumber = 1;
            PageSize = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginationFilter"/> class.
        /// </summary>
        /// <param name="pageNumber">Page size with limit set in the config.</param>
        /// <param name="pageSize">Page size 1 or above.</param>
        /// <param name="maxPageSize">Max page size.</param>
        public PaginationFilter(int pageNumber, int pageSize, int maxPageSize = 10)
        {
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize > maxPageSize ? maxPageSize : pageSize;
        }

        /// <summary>
        /// Gets or sets page number.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets page size.
        /// </summary>
        public int? PageSize { get; set; }
    }
}
