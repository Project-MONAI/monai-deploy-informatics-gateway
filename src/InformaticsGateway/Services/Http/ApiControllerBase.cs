using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    /// <summary>
    /// Base Api Controller.
    /// </summary>
    [ApiController]
    public class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiControllerBase"/> class.
        /// </summary>
        public ApiControllerBase()
        {
        }

        /// <summary>
        /// Gets internal Server Error 500.
        /// </summary>
        public static int InternalServerError => (int)HttpStatusCode.InternalServerError;

        /// <summary>
        /// Gets bad Request 400.
        /// </summary>
        public static new int BadRequest => (int)HttpStatusCode.BadRequest;

        /// <summary>
        /// Gets notFound 404.
        /// </summary>
        public static new int NotFound => (int)HttpStatusCode.NotFound;
    }
}
