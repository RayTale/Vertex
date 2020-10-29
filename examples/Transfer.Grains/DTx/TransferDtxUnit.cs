using System.Threading.Tasks;
using Transfer.IGrains.DTx;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Actor;

namespace Transfer.Grains.DTx
{
    [EventStorage(Consts.CoreDbName, nameof(TransferDtxUnit), 3)]
    [EventArchive(Consts.CoreDbName, nameof(TransferDtxUnit), "month")]
    [SnapshotStorage(Consts.CoreDbName, nameof(TransferDtxUnit), 3)]
    [NoStream]
    public class TransferDtxUnit : DTxUnitActor<long, TransferRequest, bool>, ITransferDtxUnit
    {
        public override string FlowId(TransferRequest request) => request.Id;

        public override async Task<bool> Work(TransferRequest request)
        {
            try
            {
                var result = await this.GrainFactory.GetGrain<IDTxAccount>(request.FromId).Transfer(request.ToId, request.Amount);
                if (result)
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
