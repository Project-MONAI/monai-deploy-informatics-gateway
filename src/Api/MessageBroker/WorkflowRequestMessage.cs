using System;
using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
{
    public class WorkflowRequestMessage
    {
        /// <summary>
        /// Gets or sets the name of bucket where the payload is stored.
        /// </summary>
        public string Bucket { get; set; }

        /// <summary>
        /// Gets or sets the ID of the payload which is also used as the root path of the payload.
        /// </summary>
        public Guid PayloadId { get; set; }

        /// <summary>
        /// Gets or sets the associated workflows to be launched.
        /// </summary>
        public IEnumerable<string> Workflows { get; set; }

        /// <summary>
        /// Gets or sets number of files in the payload.
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// For DIMSE, the correlation ID is the UUID associated with the first DICOM association received. For an ACR inference request, the correlation ID is the Transaction ID in the original request.
        /// </summary>
        public string CorrelationId { get; set; }
    }
}
