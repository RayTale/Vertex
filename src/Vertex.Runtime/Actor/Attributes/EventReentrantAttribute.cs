using System;

namespace Vertex.Runtime.Actor.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EventReentrantAttribute : Attribute
    {
    }
}
