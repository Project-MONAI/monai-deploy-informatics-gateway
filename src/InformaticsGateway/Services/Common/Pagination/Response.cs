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

namespace Monai.Deploy.InformaticsGateway.Services.Common.Pagination
{
    /// <summary>
    /// Response object.
    /// </summary>
    /// <typeparam name="T">Type of response data.</typeparam>
    public class Response<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Response{T}"/> class.
        /// </summary>
        public Response()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Response{T}"/> class.
        /// </summary>
        /// <param name="data">Response data.</param>
        public Response(T data)
        {
            Succeeded = true;
            Message = string.Empty;
            Errors = Array.Empty<string>();
            Data = data;
        }

        /// <summary>
        /// Gets or sets Data.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether response has succeeded.
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Gets or sets errors.
        /// </summary>
        public string[]? Errors { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets message.
        /// </summary>
        public string? Message { get; set; }
    }
}
