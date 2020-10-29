using System;

namespace Vertex.Abstractions.Exceptions
{
    public class MissingAttributeException : Exception
    {
        public MissingAttributeException(string message)
            : base(message)
        {
        }
    }
}
