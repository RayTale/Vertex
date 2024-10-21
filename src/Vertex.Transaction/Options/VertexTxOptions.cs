using Orleans;

namespace Vertex.Transaction.Options
{
    [GenerateSerializer]
    public class VertexTxOptions
    {
        /// <summary>
        /// Transaction timeout time(default is 30s)
        /// </summary>
        [Id(0)]
        public int TxSecondsTimeout { get; set; } = 30;
    }
}
