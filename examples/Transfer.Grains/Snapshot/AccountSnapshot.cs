using Vertex.Abstractions.Serialization;
using Vertex.Transaction.Abstractions.Snapshot;

namespace Transfer.Grains.Snapshot
{
    public class AccountSnapshot : ITxSnapshot<AccountSnapshot>
    {
        public decimal Balance { get; set; }

        public AccountSnapshot Clone(ISerializer serializer)
        {
            return new AccountSnapshot
            {
                Balance = this.Balance
            };
        }
    }
}
