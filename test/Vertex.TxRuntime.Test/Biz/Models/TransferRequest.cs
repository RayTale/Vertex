namespace Vertex.TxRuntime.Test.Biz.Models
{
    public class TransferRequest
    {
        public string Id { get; set; }

        public long FromId { get; set; }

        public long ToId { get; set; }

        public decimal Amount { get; set; }

        /// <summary>
        /// only for test
        /// </summary>
        public bool Success { get; set; }
    }
}
