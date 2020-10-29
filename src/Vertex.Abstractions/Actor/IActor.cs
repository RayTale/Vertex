using System.Threading.Tasks;

namespace Vertex.Abstractions.Actor
{
    public interface IActor<TPrimaryKey>
    {
        TPrimaryKey ActorId { get; }

        Task OnActivateAsync();
    }
}
