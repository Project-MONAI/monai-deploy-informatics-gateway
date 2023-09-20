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
