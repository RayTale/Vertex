namespace Vertex.Transaction.Options
{
    public class VertexTxOptions
    {
        /// <summary>
        /// Transaction timeout time(default is 30s)
        /// </summary>
        public int TxSecondsTimeout { get; set; } = 30;
    }
}
