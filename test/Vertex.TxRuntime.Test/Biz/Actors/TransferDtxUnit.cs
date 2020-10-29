using System.Threading.Tasks;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Actor;
using Vertex.TxRuntime.Core;
using Vertex.TxRuntime.Test.Biz.IActors;
using Vertex.TxRuntime.Test.Biz.Models;

namespace Vertex.TxRuntime.Test.Biz.Actors
{
    [EventStorage(TestSiloConfigurations.TestConnectionName, nameof(TransferDtxUnit), 3)]
    [EventArchive(TestSiloConfigurations.TestConnectionName, nameof(TransferDtxUnit), "month")]
    [SnapshotStorage(TestSiloConfigurations.TestConnectionName, nameof(TransferDtxUnit), 3)]
    [NoStream]
    public class TransferDtxUnit : DTxUnitActor<long, TransferRequest, bool>, ITransferDtxUnit
    {
        public override string FlowId(TransferRequest request) => request.Id;

        public override async Task<bool> Work(TransferRequest request)
        {
            try
            {
                var result = await this.GrainFactory.GetGrain<IDTxAccount>(request.FromId).Transfer(request.ToId, request.Amount);
                if (result && request.Success)
                {
                    await this.GrainFactory.GetGrain<IDTxAccount>(request.ToId).TransferArrived(request.Amount);
                    await this.Commit();
                    return true;
                }
                else
                {
                    await this.Rollback();
                    return false;
                }
            }
            catch
            {
                await this.Rollback();
                throw;
            }
        }

        protected override IDTxActor[] EffectActors(TransferRequest request)
        {
            return new IDTxActor[]
            {
                this.GrainFactory.GetGrain<IDTxAccount>(request.FromId),
                this.GrainFactory.GetGrain<IDTxAccount>(request.ToId),
            };
        }
    }
}
