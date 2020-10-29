using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Vertex.Utils
{
    public sealed class ManualResetValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> core; // mutable struct; do not make this readonly

        public bool RunContinuationsAsynchronously { get => this.core.RunContinuationsAsynchronously; set => this.core.RunContinuationsAsynchronously = value; }

        public short Version => this.core.Version;

        public void Reset() => this.core.Reset();

        public void SetResult(T result) => this.core.SetResult(result);

        public void SetException(Exception error) => this.core.SetException(error);

        public T GetResult(short token) => this.core.GetResult(token);

        void IValueTaskSource.GetResult(short token) => this.core.GetResult(token);

        public ValueTaskSourceStatus GetStatus(short token) => this.core.GetStatus(token);

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => this.core.OnCompleted(continuation, state, token, flags);

        public ValueTask<T> AsValueTask() => new ValueTask<T>(this, this.core.Version);

        public Task<T> AsTask() => this.AsValueTask().AsTask();
    }
}
