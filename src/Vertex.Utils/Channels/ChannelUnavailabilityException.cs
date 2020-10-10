using System;

namespace Vertex.Utils.Channels
{
    public class ChannelUnavailabilityException : Exception
    {
        public ChannelUnavailabilityException(string msg)
            : base(msg)
        {
        }
    }
}
