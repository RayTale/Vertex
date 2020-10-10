using System;

namespace Vertex.Stream.Common
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class NoStreamAttribute : Attribute
    {
    }
}
