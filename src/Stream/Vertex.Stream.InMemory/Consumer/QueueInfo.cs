using System;

namespace Vertex.Stream.InMemory.Consumer
{
    public class QueueInfo
    {
        public Type ActorType { get; set; }
        public string Name { get; set; }
        public string Topic { get; set; }
        public string Group { get; set; }
        public override string ToString()
        {
            return $"{Name}_{Topic}_{Group}";
        }
    }
}
