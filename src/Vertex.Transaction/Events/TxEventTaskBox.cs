using System;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;

namespace Vertex.Transaction.Events
{
    public class TxEventTaskBox<TSnapshot>
    {
        private readonly TaskCompletionSource<bool> taskCompletionSource;

        public TxEventTaskBox(
            string transactionId,
            string flowId,
            Func<TSnapshot, Func<IEvent, Task>, Task> handler,
            TaskCompletionSource<bool> taskCompletionSource)
        {
            this.TxId = transactionId;
            this.FlowId = flowId;
            this.Handler = handler;
            this.taskCompletionSource = taskCompletionSource;
        }

        public string TxId { get; set; }

        public string FlowId { get; set; }

        public bool Executed { get; set; }

        public Func<TSnapshot, Func<IEvent, Task>, Task> Handler { get; }

        public void Completed(bool result)
        {
            this.taskCompletionSource.TrySetResult(result);
        }

        public void Exception(Exception ex)
        {
            this.taskCompletionSource.TrySetException(ex);
        }
    }
}
