using Transfer.Grains.Events;
using Vertex.Runtime.Snapshot;

namespace Transfer.Grains.Snapshot
{
    public class AccountSnapshotHandler : SnapshotHandlerBase<long, AccountSnapshot>
    {
        public void EventHandle(AccountSnapshot state, TopupEvent evt)
        {
            state.Balance = evt.Balance;
        }

        public void EventHandle(AccountSnapshot state, TransferArrivedEvent evt)
        {
            state.Balance = evt.Balance;
        }

        public void EventHandle(AccountSnapshot state, TransferEvent evt)
        {
            state.Balance = evt.Balance;
        }

        public void EventHandle(AccountSnapshot state, TransferRefundsEvent evt)
        {
            state.Balance = evt.Balance;
        }
    }
}
