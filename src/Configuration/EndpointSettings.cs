using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class EndpointSettings
    {
        [ConfigurationKeyName("defaultPageSize")]
        public int DefaultPageSize { get; set; } = 10;

        [ConfigurationKeyName("maxPageSize")]
        public int MaxPageSize { get; set; } = 10;
    }
}
