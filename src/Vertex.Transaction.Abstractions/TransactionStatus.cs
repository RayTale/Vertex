namespace Vertex.Transaction.Abstractions
{
    /// <summary>
    /// 事务状态
    /// </summary>
    public enum TransactionStatus : byte
    {
        None = 0,
        WaitingCommit = 1,
        Commited = 2,
        Successed = 3,
        Rollbacked = 4
    }
}
