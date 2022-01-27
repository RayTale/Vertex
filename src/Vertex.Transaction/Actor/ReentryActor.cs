using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime;
using Vertex.Transaction.Abstractions.Snapshot;
using Vertex.Transaction.Events;
using Vertex.Utils.Channels;

namespace Vertex.Transaction.Actor
{
    public class ReentryActor<TPrimaryKey, T> : InnerTxActor<TPrimaryKey, T>
        where T : ITxSnapshot<T>, new()
    {
        protected IMpscChannel<TxEventTaskBox<SnapshotUnit<TPrimaryKey, T>>> RequestChannel { get; private set; }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            this.RequestChannel = this.ServiceProvider.GetService<IMpscChannel<TxEventTaskBox<SnapshotUnit<TPrimaryKey, T>>>>();
            this.RequestChannel.BindConsumer(this.RequestExecutor);
        }

        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
            this.RequestChannel.Dispose();
        }

        protected override Task<bool> RaiseEvent(IEvent @event, string flowId = null)
        {
            return this.ConcurrentRaiseEvent(@event, flowId);
        }

        protected virtual async Task<bool> ConcurrentRaiseEvent(
          Func<SnapshotUnit<TPrimaryKey, T>, Func<IEvent, Task>, Task> handler, string flowId = default)
        {
            if (string.IsNullOrEmpty(flowId))
            {
                flowId = RequestContext.Get(RuntimeConsts.EventFlowIdKey) as string;
                if (string.IsNullOrEmpty(flowId))
                {
                    throw new ArgumentNullException(nameof(flowId));
                }
            }
            var taskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await this.RequestChannel.WriteAsync(new TxEventTaskBox<SnapshotUnit<TPrimaryKey, T>>(default, flowId, handler, taskSource));
            return await taskSource.Task;
        }

        /// <summary>
        /// Concurrent processing of events that do not depend on the current state
        /// If the generation of the event depends on the current state, please use <see cref="ConcurrentRaiseEvent(Func{SnapshotUnit{TPrimaryKey, T}, Func{IEvent, Task}, Task}, string)"/>
        /// </summary>
        /// <param name="evt">events that do not depend on the current state</param>
        /// <param name="flowId">Idempotency judgment value</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected Task<bool> ConcurrentRaiseEvent(IEvent evt, string flowId = default)
        {
            return this.ConcurrentRaiseEvent(async (_, eventFunc) => await eventFunc(evt), flowId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnConcurrentExecuted() => ValueTask.CompletedTask;

        protected virtual Task RequestExecutor(List<TxEventTaskBox<SnapshotUnit<TPrimaryKey, T>>> requests)
        {
            if (requests.Count > 0)
            {
                return this.AutoTransactionExcute(requests);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private async Task AutoTransactionExcute(List<TxEventTaskBox<SnapshotUnit<TPrimaryKey, T>>> requests)
        {
            if (this.Logger.IsEnabled(LogLevel.Trace))
            {
                this.Logger.LogTrace("AutoTx: {0}->{1}->{2}", this.ActorType.FullName, this.ActorId.ToString(), requests.Count.ToString());
            }

            await this.BeginTransaction();
            try
            {
                foreach (var request in requests)
                {
                    await request.Handler(this.Snapshot, async (evt) =>
                     {
                         await this.TxRaiseEvent(evt, request.FlowId, request.TxId);
                     });
                }

                await this.Commit();
                await this.Finish();
                foreach (var input in requests)
                {
                    input.Completed(true);
                }
            }
            catch (Exception batchEx)
            {
                this.Logger.LogError(batchEx, batchEx.Message);
                try
                {
                    await this.Rollback();
                    await this.ReTry(requests);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, ex.Message);
                    requests.ForEach(input => input.Exception(ex));
                }
            }

            await this.OnConcurrentExecuted();
        }

        private async Task ReTry(List<TxEventTaskBox<SnapshotUnit<TPrimaryKey, T>>> requests)
        {
            foreach (var request in requests)
            {
                try
                {
                    await request.Handler(this.Snapshot, async (evt) =>
                    {
                        var result = await base.RaiseEvent(evt, request.FlowId);
                        request.Completed(result);
                    });
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, ex.Message);
                    request.Exception(ex);
                }
            }
        }
    }
}
