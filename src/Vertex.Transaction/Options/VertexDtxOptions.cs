using Orleans;

namespace Vertex.Transaction.Options
{
    [GenerateSerializer]
    public class VertexDtxOptions
    {
        [Id(0)]
        public int RetainedTxEvents { get; set; } = 30;
    }
}
