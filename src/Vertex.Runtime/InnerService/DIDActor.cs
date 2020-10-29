using System.Threading.Tasks;
using IdGen;
using Orleans;
using Orleans.Concurrency;
using Vertex.Abstractions.InnerService;

namespace Vertex.Runtime.InnerService
{
    [Reentrant]
    public class DIDActor : Grain, IDIDActor
    {
        private IdGenerator UnitIdGen { get; } = new IdGenerator(0);

        public Task<long> NewID()
        {
            return Task.FromResult(this.UnitIdGen.CreateId());
        }
    }
}
