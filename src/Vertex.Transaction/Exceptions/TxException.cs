using System;

namespace Vertex.Transaction.Exceptions
{
    public class TxException : Exception
    {
        public TxException(string msg)
            : base(msg)
        {
        }
    }
}
