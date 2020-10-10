using Microsoft.Extensions.ObjectPool;

namespace Vertex.Utils.PooledTask
{
    public class TaskSourcePool<T> : DefaultObjectPool<ManualResetValueTaskSource<T>>
    {
        public TaskSourcePool() : base(new PooledTaskSourcePolicy<T>())
        {
        }
    }
}
