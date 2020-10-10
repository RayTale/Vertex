using System;

namespace Vertex.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StrictHandleAttributer : Attribute
    {
    }
}
