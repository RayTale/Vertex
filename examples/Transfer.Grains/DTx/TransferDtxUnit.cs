using System.Threading.Tasks;
using Transfer.IGrains.DTx;
using Vertex.Storage.Linq2db.Core;
using Vertex.Stream.Common;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Actor;

namespace Transfer.Grains.DTx
{
    [EventStorage(Consts.core_db_Name, nameof(TransferDtxUnit), 3)]
    [EventArchive(Consts.core_db_Name, nameof(TransferDtxUnit), "month")]
    [SnapshotStorage(Consts.core_db_Name, nameof(TransferDtxUnit), 3)]
    [NoStream]
    public class TransferDtxUnit : DTxUnitActor<long, TransferRequest, bool>, ITransferDtxUnit
    {
        public override string FlowId(TransferRequest request) => request.Id;

        public override async Task<bool> Work(TransferRequest request)
        {
            try
            {
                var result = await GrainFactory.GetGrain<IDTxAccount>(request.FromId).Transfer(request.ToId, request.Amount);
                if (result)
                {
                    await GrainFactory.GetGrain<IDTxAccount>(request.ToId).TransferArrived(request.Amount);
                    await Commit();
                    return true;
                }
                else
                {
                    await Rollback();
                    return false;
                }
            }
            catch
            {
                await Rollback();
                throw;
            }
        }
        protected override IDTxActor[] EffectActors(TransferRequest request)
        {
            return new IDTxActor[]
            {
                GrainFactory.GetGrain<IDTxAccount>(request.FromId),
                GrainFactory.GetGrain<IDTxAccount>(request.ToId),
            };
        }
    }
}
