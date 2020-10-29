namespace Transfer.IGrains.DTx
{
    public class TransferRequest
    {
        public string Id { get; set; }

        public long FromId { get; set; }

        public long ToId { get; set; }

        public decimal Amount { get; set; }
    }
}
