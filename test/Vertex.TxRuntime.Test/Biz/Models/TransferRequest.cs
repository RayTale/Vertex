using Orleans;

namespace Vertex.TxRuntime.Test.Biz.Models
{
    [GenerateSerializer]
    public class TransferRequest
    {
        [Id(0)]
        public string Id { get; set; }

        [Id(1)]
        public long FromId { get; set; }

        [Id(2)]
        public long ToId { get; set; }

        [Id(3)]
        public decimal Amount { get; set; }

        /// <summary>
        /// only for test
        /// </summary>
        [Id(4)]
        public bool Success { get; set; }
    }
}
