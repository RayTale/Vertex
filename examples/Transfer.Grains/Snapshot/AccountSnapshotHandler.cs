using Transfer.Grains.Events;
using Transfer.Repository.Entities;
using Vertex.Runtime.Snapshot;

namespace Transfer.Grains.Snapshot
{
    public class AccountSnapshotHandler : SnapshotHandlerBase<long, AccountSnapshot>
    {
        public void EventHandle(AccountSnapshot state, CreateEvent evt)
        {
            EntityHandle(state, evt);
        }

        public void EntityHandle(Account entity, CreateEvent evt)
        {
            entity.Balance = evt.Balance;
        }

        public void EventHandle(AccountSnapshot state, TopupEvent evt)
        {
            EntityHandle(state, evt);
        }

        public void EntityHandle(Account entity, TopupEvent evt)
        {
            entity.Balance = evt.Balance;
        }

        public void EventHandle(AccountSnapshot state, TransferArrivedEvent evt)
        {
            EntityHandle(state, evt);
        }

        public void EntityHandle(Account entity, TransferArrivedEvent evt)
        {
            entity.Balance = evt.Balance;
        }

        public void EventHandle(AccountSnapshot state, TransferEvent evt)
        {
            EntityHandle(state, evt);
        }

        public void EntityHandle(Account entity, TransferEvent evt)
        {
            entity.Balance = evt.Balance;
        }

        public void EventHandle(AccountSnapshot state, TransferRefundsEvent evt)
        {
            EntityHandle(state, evt);
        }

        public void EntityHandle(Account entity, TransferRefundsEvent evt)
        {
            entity.Balance = evt.Balance;
        }
    }
}
