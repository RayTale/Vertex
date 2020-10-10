using System;
using System.Collections.Generic;

namespace Vertex.Stream.RabbitMQ.Consumer
{
    public class QueueInfo
    {
        public List<Type> SubActorType { get; set; }
        public string Exchange { get; set; }
        public string Queue { get; set; }
        public string RoutingKey { get; set; }

        public override string ToString()
        {
            return $"{this.Queue}_{this.RoutingKey}";
        }
    }
}
