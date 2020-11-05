using Transfer.Repository.Entities;
using Vertex.Abstractions.Serialization;
using Vertex.Transaction.Abstractions.Snapshot;

namespace Transfer.Grains.Snapshot
{
    public class AccountSnapshot : Account, ITxSnapshot<AccountSnapshot>
    {
        public AccountSnapshot Clone(ISerializer serializer)
        {
            return new AccountSnapshot
            {
                Balance = this.Balance
            };
        }
    }
}
