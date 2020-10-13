using Orleans;
using Vertex.Transaction.Abstractions.IActor;

namespace Transfer.IGrains.DTx
{
    public interface ITransferDtxUnit : IDTxUnitActor<TransferRequest, bool>, IGrainWithIntegerKey
    {
    }
}
