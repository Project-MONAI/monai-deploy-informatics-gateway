
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Database.Api.Repositories
{
    public interface IRemoteAppExecutionRepository
    {
        Task<bool> AddAsync(RemoteAppExecution item, CancellationToken cancellationToken = default);

        Task<RemoteAppExecution?> GetAsync(string OutgoingStudyUid, CancellationToken cancellationToken = default);

        Task<int> RemoveAsync(string OutgoingStudyUid, CancellationToken cancellationToken = default);
    }
}
