using Orleans;

namespace Transfer.IGrains.DTx
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
    }
}
