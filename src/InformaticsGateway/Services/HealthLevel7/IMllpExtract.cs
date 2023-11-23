

using System.Threading.Tasks;
using HL7.Dotnetcore;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal interface IMllpExtract
    {
        Task<Message> ExtractInfo(Hl7FileStorageMetadata meta, Message message);
    }
}
