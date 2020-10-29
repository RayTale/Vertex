namespace Vertex.Transaction.Abstractions
{
    public class DTxCommit<TInput>
    {
        public string TxId { get; set; }

        public TransactionStatus Status { get; set; }

        public TInput Data { get; set; }

        public long Timestamp { get; set; }
    }
}
