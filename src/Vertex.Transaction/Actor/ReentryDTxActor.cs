using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Snapshot;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Abstractions.Snapshot;
using Vertex.Transaction.Abstractions.Storage;
using Vertex.Transaction.Events;
using Vertex.Transaction.Options;

namespace Vertex.Transaction.Actor
{
    public class ReentryDTxActor<TPrimaryKey, T> : ReentryActor<TPrimaryKey, T>, IDTxActor
          where T : ITxSnapshot<T>, new()
    {
        /// <summary>
        /// Event storage
        /// </summary>
        protected ITxEventStorage<TPrimaryKey> TxEventStorage { get; private set; }

        protected VertexDtxOptions DtxOptions { get; private set; }

        private long CurrentTxEventVersion { get; set; }

        protected long ActivateTxEventVersion { get; private set; }

        protected async override ValueTask DependencyInjection()
        {
            this.DtxOptions = this.ServiceProvider.GetService<IOptionsMonitor<VertexDtxOptions>>().Get(this.ActorType.FullName);
            var txEventStorageFactory = this.ServiceProvider.GetService<ITxEventStorageFactory>();
            this.TxEventStorage = await txEventStorageFactory.Create(this);
            await base.DependencyInjection();
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            var txEvents = this.Convert(await this.TxEventStorage.GetLatest(this.ActorId, this.DtxOptions.RetainedTxEvents));
            if (txEvents.Count > 0)
            {
                foreach (var evt in txEvents.OrderBy(o => o.Meta.Version))
                {
                    this.TxEventApply(evt);
                }
                await this.TxEventStorage.DeletePrevious(this.ActorId, this.CurrentTxEventVersion - this.DtxOptions.RetainedTxEvents);
                this.ActivateTxEventVersion = this.CurrentTxEventVersion;
            }
            if (!string.IsNullOrEmpty(this.TxSnapshot.TxId))
            {
                await this.TxBeginLock.WaitAsync();
                var documents = await this.EventStorage.GetList(this.ActorId, this.TxSnapshot.TxStartVersion, this.Snapshot.Meta.Version);
                var waitingEvents = this.Convert(documents);
                foreach (var evt in waitingEvents)
                {
                    var transport = new EventBufferUnit<TPrimaryKey>(evt, this.EventTypeContainer, this.Serializer);
                    this.WaitingCommitEventList.Add(transport);
                }
            }
        }

        protected override Task RequestExecutor(List<TxEventTaskBox<SnapshotUnit<TPrimaryKey, T>>> requests)
        {
            var autoTransactionList = requests.Where(r => string.IsNullOrEmpty(r.TxId)).ToList();

            return Task.WhenAll(base.RequestExecutor(autoTransactionList), Executor(requests));

            async Task Executor(List<TxEventTaskBox<SnapshotUnit<TPrimaryKey, T>>> list)
            {
                foreach (var request in list.Where(r => !string.IsNullOrEmpty(r.TxId)))
                {
                    try
                    {
                        await request.Handler(this.Snapshot, async (evt) =>
                        {
                            await this.TxRaiseEvent(evt, request.FlowId, request.TxId);
                            request.Executed = true;
                        });
                        request.Completed(request.Executed);
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(ex, ex.Message);
                        request.Exception(ex);
                    }
                }
            }
        }

        public Task<bool> Commit()
        {
            var txId = RequestContext.Get(RequestContextKeys.TxIdKey) as string;
            if (string.IsNullOrEmpty(txId))
            {
                throw new ArgumentNullException(nameof(txId));
            }

            return this.Commit(txId);
        }

        public Task Finish()
        {
            var txId = RequestContext.Get(RequestContextKeys.TxIdKey) as string;
            if (string.IsNullOrEmpty(txId))
            {
                throw new ArgumentNullException(nameof(txId));
            }
            return this.Finish(txId);
        }

        public Task Rollback()
        {
            var txId = RequestContext.Get(RequestContextKeys.TxIdKey) as string;
            if (string.IsNullOrEmpty(txId))
            {
                throw new ArgumentNullException(nameof(txId));
            }
            return this.Rollback(txId);
        }

        protected override async Task<bool> ConcurrentRaiseEvent(Func<SnapshotUnit<TPrimaryKey, T>, Func<IEvent, Task>, Task> handler, string flowId = default)
        {
            var txId = RequestContext.Get(RequestContextKeys.TxIdKey) as string;
            var taskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await this.RequestChannel.WriteAsync(new TxEventTaskBox<SnapshotUnit<TPrimaryKey, T>>(txId, flowId, handler, taskSource));
            return await taskSource.Task;
        }

        protected override async ValueTask TxRaiseEvent(IEvent @event, string flowId = default, string txId = default)
        {
            if (txId == default)
            {
                txId = RequestContext.Get(RequestContextKeys.TxIdKey) as string;
            }

            if (txId != this.TxSnapshot.TxId)
            {
                await this.BeginTransaction(txId);
            }
            await base.TxRaiseEvent(@event, flowId, txId);
        }

        protected override async ValueTask OnTxCommit(string txId)
        {
            // If it is a transaction with Id, join the transaction event and wait for Complete
            if (!string.IsNullOrEmpty(txId) && this.EventTypeContainer.TryGet(typeof(TxCommitEvent), out var eventName))
            {
                var commitEvent = new EventUnit<TPrimaryKey>
                {
                    Event = new TxCommitEvent { Id = this.TxSnapshot.TxId, StartVersion = this.TxSnapshot.TxStartVersion, StartTime = this.TxSnapshot.TxStartTime },
                    Meta = new EventMeta { Version = this.CurrentTxEventVersion + 1, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), FlowId = txId },
                    ActorId = this.ActorId
                };
                await this.TxEventStorage.Append(new EventDocument<TPrimaryKey>
                {
                    FlowId = commitEvent.Meta.FlowId,
                    ActorId = this.ActorId,
                    Data = this.Serializer.Serialize(commitEvent.Event as TxCommitEvent),
                    Name = eventName,
                    Timestamp = commitEvent.Meta.Timestamp,
                    Version = commitEvent.Meta.Version
                });
                this.TxEventApply(commitEvent);
            }
        }

        protected override async ValueTask OnTxCommited(string txId)
        {
            // If it is a transaction with Id, join the transaction event and wait for Complete
            if (!string.IsNullOrEmpty(txId) && this.EventTypeContainer.TryGet(typeof(TxCommitedEvent), out var eventName))
            {
                var commitEvent = new EventUnit<TPrimaryKey>
                {
                    Event = new TxCommitedEvent { Id = this.TxSnapshot.TxId },
                    Meta = new EventMeta { Version = this.CurrentTxEventVersion + 1, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), FlowId = txId },
                    ActorId = this.ActorId
                };
                await this.TxEventStorage.Append(new EventDocument<TPrimaryKey>
                {
                    FlowId = txId,
                    ActorId = this.ActorId,
                    Data = this.Serializer.Serialize(commitEvent.Event as TxCommitedEvent),
                    Name = eventName,
                    Timestamp = commitEvent.Meta.Timestamp,
                    Version = commitEvent.Meta.Version
                });
                this.TxEventApply(commitEvent);
                await this.ClearTxEvents();
            }
        }

        protected override async ValueTask OnTxFinsh(string txId)
        {
            if (!string.IsNullOrEmpty(txId) && this.EventTypeContainer.TryGet(typeof(TxFinishedEvent), out var eventName))
            {
                var finishEvent = new EventUnit<TPrimaryKey>
                {
                    Event = new TxFinishedEvent { Id = this.TxSnapshot.TxId },
                    Meta = new EventMeta { Version = this.CurrentTxEventVersion + 1, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), FlowId = txId },
                    ActorId = this.ActorId
                };
                await this.TxEventStorage.Append(new EventDocument<TPrimaryKey>
                {
                    FlowId = txId,
                    ActorId = this.ActorId,
                    Data = this.Serializer.Serialize(finishEvent.Event as TxFinishedEvent),
                    Name = eventName,
                    Timestamp = finishEvent.Meta.Timestamp,
                    Version = finishEvent.Meta.Version
                });
                this.TxEventApply(finishEvent);
                await this.ClearTxEvents();
            }
        }

        protected override async ValueTask OnTxRollback(string txId)
        {
            if (!string.IsNullOrEmpty(txId) && this.EventTypeContainer.TryGet(typeof(TxFinishedEvent), out var eventName))
            {
                var rollbackEvent = new EventUnit<TPrimaryKey>
                {
                    Event = new TxRollbackEvent { Id = this.TxSnapshot.TxId },
                    Meta = new EventMeta { Version = this.CurrentTxEventVersion + 1, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), FlowId = txId },
                    ActorId = this.ActorId
                };
                await this.TxEventStorage.Append(new EventDocument<TPrimaryKey>
                {
                    FlowId = txId,
                    ActorId = this.ActorId,
                    Data = this.Serializer.Serialize(rollbackEvent.Event as TxFinishedEvent),
                    Name = eventName,
                    Timestamp = rollbackEvent.Meta.Timestamp,
                    Version = rollbackEvent.Meta.Version
                });
                this.TxEventApply(rollbackEvent);
                await this.ClearTxEvents();
            }
        }

        private void TxEventApply(EventUnit<TPrimaryKey> evt)
        {
            switch (evt.Event)
            {
                case TxCommitEvent value:
                    {
                        this.TxSnapshot.TxId = value.Id;
                        this.TxSnapshot.TxStartVersion = value.StartVersion;
                        this.TxSnapshot.TxStartTime = evt.Meta.Timestamp;
                        this.TxSnapshot.Status = TransactionStatus.WaitingCommit;
                    }; break;
                case TxCommitedEvent _:
                    {
                        this.TxSnapshot.Status = TransactionStatus.Commited;
                    }; break;
                case TxFinishedEvent _:
                    {
                        this.TxSnapshot.Reset();
                    }; break;
                case TxRollbackEvent _:
                    {
                        this.TxSnapshot.Reset();
                    }; break;
                default: throw new NotSupportedException(evt.Event.GetType().FullName);
            }
            this.CurrentTxEventVersion = evt.Meta.Version;
        }

        protected async ValueTask ClearTxEvents()
        {
            if (this.CurrentTxEventVersion - this.ActivateTxEventVersion > this.DtxOptions.RetainedTxEvents)
            {
                await this.TxEventStorage.DeletePrevious(this.ActorId, this.CurrentTxEventVersion - this.DtxOptions.RetainedTxEvents);
                this.ActivateTxEventVersion = this.CurrentTxEventVersion;
            }
        }
    }
}
