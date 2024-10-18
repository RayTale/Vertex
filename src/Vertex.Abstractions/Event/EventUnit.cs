using Orleans;

namespace Vertex.Abstractions.Event
{
    /// <summary>
    /// A typed wrapper for an event that contains details about the event.
    /// </summary>
    /// <typeparam name="TPrimaryKey">The type of the entity's key.</typeparam>
    [GenerateSerializer]
    public class EventUnit<TPrimaryKey>
    {
        [Id(0)]
        public TPrimaryKey ActorId { get; set; }

        [Id(1)]
        public EventMeta Meta { get; set; }
        
        public IEvent Event { get; set; }
    }
}