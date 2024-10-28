using System;
using Orleans;

namespace Vertex.Runtime.Exceptions
{
    [GenerateSerializer]
    public class VertexEventHandlerException : Exception
    {
        public VertexEventHandlerException(Type eventType)
            : base(eventType.FullName)
        {
        }
    }
}
