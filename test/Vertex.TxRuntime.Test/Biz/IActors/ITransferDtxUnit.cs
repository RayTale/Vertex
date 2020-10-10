using Orleans;
using Vertex.Transaction.Abstractions.IActor;
using Vertex.TxRuntime.Test.Biz.Models;

namespace Vertex.TxRuntime.Test.Biz.IActors
{
    public interface ITransferDtxUnit : IDTxUnitActor<TransferRequest, bool>, IGrainWithIntegerKey
    {
    }
}
