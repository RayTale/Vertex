using Orleans;
using System.Threading.Tasks;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Snapshot;
using Vertex.TxRuntime.Test.Snapshot;

namespace Vertex.TxRuntime.Test.Biz.IActors
{
    public interface IReentryAccount : IVertexActor, IGrainWithIntegerKey
    {
        Task<SnapshotUnit<long, AccountSnapshot>> GetSnapshot();
        Task<SnapshotUnit<long, AccountSnapshot>> GetBackupSnapshot();
        Task<bool> TopUp(decimal amount, string flowId);
        Task ErrorTest();
    }
}
