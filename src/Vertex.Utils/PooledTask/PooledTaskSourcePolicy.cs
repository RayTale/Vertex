using Microsoft.Extensions.ObjectPool;

namespace Vertex.Utils.PooledTask
{
    public class PooledTaskSourcePolicy<T> : IPooledObjectPolicy<ManualResetValueTaskSource<T>>
    {
        public ManualResetValueTaskSource<T> Create()
        {
            return new ManualResetValueTaskSource<T>();
        }

        public bool Return(ManualResetValueTaskSource<T> obj)
        {
            obj.Reset();
            return true;
        }
    }
}
