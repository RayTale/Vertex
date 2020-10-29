namespace Vertex.Transaction.Abstractions.Snapshot
{
    public record TxTempSnapshot
    {
        /// <summary>
        /// empty: local affairs
        /// !empty: Distributed transaction
        /// </summary>
        public string TxId { get; set; }

        /// <summary>
        /// The beginning version of the transaction
        /// </summary>
        public long TxStartVersion { get; set; }

        /// <summary>
        /// The time when the transaction started (used for timeout processing)
        /// </summary>
        public long TxStartTime { get; set; }

        public TransactionStatus Status { get; set; }

        public void Reset()
        {
            this.TxStartVersion = 0;
            this.TxId = string.Empty;
            this.TxStartTime = 0;
            this.Status = TransactionStatus.None;
        }
    }
}
