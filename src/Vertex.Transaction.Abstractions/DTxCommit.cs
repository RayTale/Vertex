namespace Vertex.Transaction.Abstractions
{
    public class DTxCommit<Input>
    {
        public string TxId { get; set; }

        public TransactionStatus Status { get; set; }

        public Input Data { get; set; }

        public long Timestamp { get; set; }
    }
    /// <summary>
    /// 事务状态
    /// </summary>
    public enum TransactionStatus
    {
        None = 0,
        WaitingCommit = 1,
        Commited = 2,
        Successed = 3,
        Rollbacked = 4 
    }
}
