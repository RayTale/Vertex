using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Snapshot;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Abstractions.Snapshot;
using Vertex.Transaction.Abstractions.Storage;
using Vertex.Transaction.Events;
using Vertex.Transaction.Options;
using Vertext.Abstractions.Event;

namespace Vertex.Transaction.Actor
{
    public class ReentryDTxActor<PrimaryKey, T> : ReentryActor<PrimaryKey, T>, IDTxActor
          where T : ITxSnapshot<T>, new()
    {
        /// <summary>
        /// Event storage
        /// </summary>
        protected ITxEventStorage<PrimaryKey> TxEventStorage { get; private set; }
        protected VertexDtxOptions DtxOptions { get; private set; }
        private long CurrentTxEventVersion { get; set; }
        protected long ActivateTxEventVersion { get; private set; }
        protected async override ValueTask DependencyInjection()
        {
            this.DtxOptions = this.ServiceProvider.GetOptionsByName<VertexDtxOptions>(this.ActorType.FullName);
            var txEventStorageFactory = this.ServiceProvider.GetService<ITxEventStorageFactory>();
            this.TxEventStorage = await txEventStorageFactory.Create(this);
            await base.DependencyInjection();
        }
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            var txEvents = Convert(await TxEventStorage.GetLatest(ActorId, DtxOptions.RetainedTxEvents));
            if (txEvents.Count > 0)
            {
                foreach (var evt in txEvents.OrderBy(o => o.Meta.Version))
                {
                    TxEventApply(evt);
                }
                await TxEventStorage.DeletePrevious(ActorId, this.CurrentTxEventVersion - DtxOptions.RetainedTxEvents);
                ActivateTxEventVersion = this.CurrentTxEventVersion;
            }
            if (!string.IsNullOrEmpty(TxSnapshot.TxId))
            {
                await this.TxBeginLock.WaitAsync();
                var documents = await EventStorage.GetList(this.ActorId, TxSnapshot.TxStartVersion, this.Snapshot.Meta.Version);
                var waitingEvents = Convert(documents);
                foreach (var evt in waitingEvents)
                {
                    var transport = new EventBufferUnit<PrimaryKey>(evt, this.EventTypeContainer, this.Serializer);
                    this.WaitingCommitEventList.Add(transport);
                }
            }
        }
        protected override Task RequestExecutor(List<TxEventTaskBox<SnapshotUnit<PrimaryKey, T>>> requests)
        {
            var autoTransactionList = requests.Where(r => string.IsNullOrEmpty(r.TxId)).ToList();

            return Task.WhenAll(base.RequestExecutor(autoTransactionList), Executor(requests));

            async Task Executor(List<TxEventTaskBox<SnapshotUnit<PrimaryKey, T>>> list)
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
            var txId = RequestContext.Get(RequestContextKeys.txIdKey) as string;
            if (string.IsNullOrEmpty(txId))
                throw new ArgumentNullException("transaction id");
            return this.Commit(txId);
        }

        public Task Finish()
        {
            var txId = RequestContext.Get(RequestContextKeys.txIdKey) as string;
            if (string.IsNullOrEmpty(txId))
                throw new ArgumentNullException("transaction id");
            return this.Finish(txId);
        }

        public Task Rollback()
        {
            var txId = RequestContext.Get(RequestContextKeys.txIdKey) as string;
            if (string.IsNullOrEmpty(txId))
                throw new ArgumentNullException("transaction id");
            return this.Rollback(txId);
        }
        protected override async Task<bool> ConcurrentRaiseEvent(Func<SnapshotUnit<PrimaryKey, T>, Func<IEvent, Task>, Task> handler, string flowId = default)
        {
            var txId = RequestContext.Get(RequestContextKeys.txIdKey) as string;
            var taskSource = new TaskCompletionSource<bool>();
            await this.RequestChannel.WriteAsync(new TxEventTaskBox<SnapshotUnit<PrimaryKey, T>>(txId, flowId, handler, taskSource));
            return await taskSource.Task;
        }

        protected override async ValueTask TxRaiseEvent(IEvent @event, string flowId = default, string txId = default)
        {
            if (txId == default)
                txId = RequestContext.Get(RequestContextKeys.txIdKey) as string;
            if (txId != TxSnapshot.TxId)
            {
                await this.BeginTransaction(txId);
            }
            await base.TxRaiseEvent(@event, flowId, txId);
        }

        protected override async ValueTask OnTxCommit(string txId)
        {
            // If it is a transaction with Id, join the transaction event and wait for Complete
            if (!string.IsNullOrEmpty(txId) && EventTypeContainer.TryGet(typeof(TxCommitEvent), out var eventName))
            {
                var commitEvent = new EventUnit<PrimaryKey>
                {
                    Event = new TxCommitEvent { Id = TxSnapshot.TxId, StartVersion = this.TxSnapshot.TxStartVersion, StartTime = this.TxSnapshot.TxStartTime },
                    Meta = new EventMeta { Version = CurrentTxEventVersion + 1, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), FlowId = txId },
                    ActorId = this.ActorId
                };
                await TxEventStorage.Append(new EventDocument<PrimaryKey>
                {
                    FlowId = commitEvent.Meta.FlowId,
                    ActorId = ActorId,
                    Data = Serializer.Serialize(commitEvent.Event as TxCommitEvent),
                    Name = eventName,
                    Timestamp = commitEvent.Meta.Timestamp,
                    Version = commitEvent.Meta.Version
                });
                TxEventApply(commitEvent);
            }
        }
        protected override async ValueTask OnTxCommited(string txId)
        {
            // If it is a transaction with Id, join the transaction event and wait for Complete
            if (!string.IsNullOrEmpty(txId) && EventTypeContainer.TryGet(typeof(TxCommitedEvent), out var eventName))
            {
                var commitEvent = new EventUnit<PrimaryKey>
                {
                    Event = new TxCommitedEvent { Id = TxSnapshot.TxId },
                    Meta = new EventMeta { Version = CurrentTxEventVersion + 1, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), FlowId = txId },
                    ActorId = this.ActorId
                };
                await TxEventStorage.Append(new EventDocument<PrimaryKey>
                {
                    FlowId = txId,
                    ActorId = ActorId,
                    Data = Serializer.Serialize(commitEvent.Event as TxCommitedEvent),
                    Name = eventName,
                    Timestamp = commitEvent.Meta.Timestamp,
                    Version = commitEvent.Meta.Version
                });
                TxEventApply(commitEvent);
                await ClearTxEvents();
            }
        }
        protected override async ValueTask OnTxFinsh(string txId)
        {
            if (!string.IsNullOrEmpty(txId) && EventTypeContainer.TryGet(typeof(TxFinishedEvent), out var eventName))
            {
                var finishEvent = new EventUnit<PrimaryKey>
                {
                    Event = new TxFinishedEvent { Id = TxSnapshot.TxId },
                    Meta = new EventMeta { Version = CurrentTxEventVersion + 1, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), FlowId = txId },
                    ActorId = this.ActorId
                };
                await TxEventStorage.Append(new EventDocument<PrimaryKey>
                {
                    FlowId = txId,
                    ActorId = ActorId,
                    Data = Serializer.Serialize(finishEvent.Event as TxFinishedEvent),
                    Name = eventName,
                    Timestamp = finishEvent.Meta.Timestamp,
                    Version = finishEvent.Meta.Version
                });
                TxEventApply(finishEvent);
                await ClearTxEvents();
            }
        }
        protected override async ValueTask OnTxRollback(string txId)
        {
            if (!string.IsNullOrEmpty(txId) && EventTypeContainer.TryGet(typeof(TxFinishedEvent), out var eventName))
            {
                var rollbackEvent = new EventUnit<PrimaryKey>
                {
                    Event = new TxRollbackEvent { Id = TxSnapshot.TxId },
                    Meta = new EventMeta { Version = CurrentTxEventVersion + 1, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), FlowId = txId },
                    ActorId = this.ActorId
                };
                await TxEventStorage.Append(new EventDocument<PrimaryKey>
                {
                    FlowId = txId,
                    ActorId = ActorId,
                    Data = Serializer.Serialize(rollbackEvent.Event as TxFinishedEvent),
                    Name = eventName,
                    Timestamp = rollbackEvent.Meta.Timestamp,
                    Version = rollbackEvent.Meta.Version
                });
                TxEventApply(rollbackEvent);
                await ClearTxEvents();
            }
        }
        private void TxEventApply(EventUnit<PrimaryKey> evt)
        {
            switch (evt.Event)
            {
                case TxCommitEvent value:
                    {
                        TxSnapshot.TxId = value.Id;
                        TxSnapshot.TxStartVersion = value.StartVersion;
                        TxSnapshot.TxStartTime = evt.Meta.Timestamp;
                        TxSnapshot.Status = TransactionStatus.WaitingCommit;
                    }; break;
                case TxCommitedEvent _:
                    {
                        TxSnapshot.Status = TransactionStatus.Commited;
                    }; break;
                case TxFinishedEvent _:
                    {
                        TxSnapshot.Reset();
                    }; break;
                case TxRollbackEvent _:
                    {
                        TxSnapshot.Reset();
                    }; break;
                default: throw new NotSupportedException(evt.Event.GetType().FullName);
            }
            this.CurrentTxEventVersion = evt.Meta.Version;
        }
        protected async ValueTask ClearTxEvents()
        {
            if (this.CurrentTxEventVersion - this.ActivateTxEventVersion > this.DtxOptions.RetainedTxEvents)
            {
                await TxEventStorage.DeletePrevious(ActorId, this.CurrentTxEventVersion - DtxOptions.RetainedTxEvents);
                ActivateTxEventVersion = this.CurrentTxEventVersion;
            }
        }
    }
}
