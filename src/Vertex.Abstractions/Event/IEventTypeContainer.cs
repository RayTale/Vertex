using System;

namespace Vertex.Abstractions.Event
{
    public interface IEventTypeContainer
    {
        bool TryGet(string name, out Type type);

        bool TryGet(Type type, out string name);
    }
}
