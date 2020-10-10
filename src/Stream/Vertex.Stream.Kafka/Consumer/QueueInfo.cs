using System;
using System.Collections.Generic;

namespace Vertex.Stream.Kafka.Consumer
{
    public class QueueInfo
    {
        public List<Type> SubActorType { get; set; }
        public string Topic { get; set; }
        public string Group { get; set; }
        public override string ToString()
        {
            return $"{Topic}_{Group}";
        }
    }
}
