namespace Vertex.Abstractions.Actor
{
    public interface IActor<PrimaryKey>
    {
        PrimaryKey ActorId { get; }
    }
}
