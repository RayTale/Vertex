using Orleans;
using Vertex.Abstractions.Serialization;
using Vertex.Transaction.Abstractions.Snapshot;

namespace Vertex.TxRuntime.Test.Snapshot
{
    /// <summary>
    /// 账户快照类
    /// </summary>
    [GenerateSerializer]
    public class AccountSnapshot : ITxSnapshot<AccountSnapshot>
    {
        /// <summary>
        /// 账户余额
        /// </summary>
        [Id(0)]
        public decimal Balance { get; set; }

        /// <summary>
        /// 克隆当前快照
        /// </summary>
        /// <param name="serializer">序列化器</param>
        /// <returns>克隆的账户快照</returns>
        public AccountSnapshot Clone(ISerializer serializer)
        {
            return new AccountSnapshot
            {
                Balance = this.Balance
            };
        }
    }
}