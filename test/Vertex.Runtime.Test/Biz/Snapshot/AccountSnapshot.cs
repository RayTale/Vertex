using Orleans;
using Vertex.Abstractions.Snapshot;

namespace Vertex.Runtime.Test.Snapshot
{
    /// <summary>
    /// 账户快照类
    /// </summary>
    [GenerateSerializer]
    public class AccountSnapshot : ISnapshot
    {
        /// <summary>
        /// 账户余额
        /// </summary>
        [Id(0)]
        public decimal Balance { get; set; }
    }
}