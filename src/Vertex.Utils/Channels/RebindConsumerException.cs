using System;

namespace Vertex.Utils.Channels
{
    public class RebindConsumerException : Exception
    {
        public RebindConsumerException(string message)
            : base(message)
        {
        }
    }
}
