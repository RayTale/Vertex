using System;

namespace Vertex.Runtime.Exceptions
{
    public class VertexEventHandlerException : Exception
    {
        public VertexEventHandlerException(Type eventType)
            : base(eventType.FullName)
        {
        }
    }
}
