namespace Vertex.Abstractions.Event
{
    /// <summary>
    /// A typed wrapper for an event that contains details about the event.
    /// </summary>
    /// <typeparam name="TPrimaryKey">The type of the entity's key.</typeparam>
    public class EventUnit<TPrimaryKey>
    {
        public IEvent Event { get; set; }

        public EventMeta Meta { get; set; }

        public TPrimaryKey ActorId { get; set; }
    }
}