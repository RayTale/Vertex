using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime.Actor;
using Vertex.Transaction.Abstractions.Snapshot;
using Vertex.Transaction.Events;
using Vertex.Transaction.Exceptions;
using Vertex.Transaction.Options;
using Vertext.Abstractions.Event;

namespace Vertex.Transaction.Actor
{
    public abstract class InnerTxActor<PrimaryKey, T> : VertexActor<PrimaryKey, T>
       where T : ITxSnapshot<T>, new()
    {
        /// <summary>
        /// Backup snapshot used for rollback during transaction
        /// </summary>
        protected SnapshotUnit<PrimaryKey, T> BackupSnapshot { get; set; }
        protected TxTempSnapshot TxSnapshot { get; set; } = new TxTempSnapshot();
        /// <summary>
        /// List of data to be submitted in the transaction
        /// </summary>
        protected List<EventBufferUnit<PrimaryKey>> WaitingCommitEventList { get; } = new List<EventBufferUnit<PrimaryKey>>();
        protected VertexTxOptions VertexTxOptions { get; private set; }
        /// <summary>
        /// Semaphore controller that guarantees that only one transaction is started at the same time
        /// </summary>
        protected SemaphoreSlim TxBeginLock { get; } = new SemaphoreSlim(1, 1);
        protected SemaphoreSlim TxTimeoutLock { get; } = new SemaphoreSlim(1, 1);
        #region Activate
        protected override ValueTask DependencyInjection()
        {
            this.VertexTxOptions = this.ServiceProvider.GetOptionsByName<VertexTxOptions>(this.ActorType.FullName);
            return base.DependencyInjection();
        }
        protected override async Task RecoverySnapshot()
        {
            await base.RecoverySnapshot();
            this.BackupSnapshot = this.Snapshot with { Data = this.Snapshot.Data.Clone(Serializer), Meta = this.Snapshot.Meta with { } };
        }
        #endregion
        protected async Task BeginTransaction(string txId = default)
        {
            if (this.Logger.IsEnabled(LogLevel.Trace))
            {
                this.Logger.LogTrace("Tx begin: {0}->{1}->{2}", this.ActorType.Name, this.ActorId.ToString(), txId);
            }

            if (TxTimeout())
            {
                if (await this.TxTimeoutLock.WaitAsync(this.VertexTxOptions.TxSecondsTimeout * 1000))
                {
                    try
                    {
                        if (TxTimeout())
                        {
                            if (this.Logger.IsEnabled(LogLevel.Information))
                            {
                                this.Logger.LogInformation("Tx timeout: {0}->{1}->{2}", this.ActorType.Name, this.ActorId.ToString(), txId);
                            }
                            await this.Rollback(TxSnapshot.TxId);//Automatic rollback of transaction timeout
                        }
                    }
                    finally
                    {
                        this.TxTimeoutLock.Release();
                    }
                }
            }

            if (await this.TxBeginLock.WaitAsync(this.VertexTxOptions.TxSecondsTimeout * 1000))
            {
                try
                {
                    this.SnapshotCheck();
                    TxSnapshot.TxStartVersion = this.Snapshot.Meta.Version + 1;
                    TxSnapshot.TxId = txId;
                    TxSnapshot.TxStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    TxSnapshot.Status = Abstractions.TransactionStatus.WaitingCommit;
                }
                catch (Exception ex)
                {
                    this.TxBeginLock.Release();
                    this.Logger.LogCritical(ex, ex.Message);
                    throw;
                }
            }
            else
            {
                throw new TimeoutException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TxTimeout() => TxSnapshot.TxStartTime != 0
            && TxSnapshot.Status < Abstractions.TransactionStatus.Commited
            && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - TxSnapshot.TxStartTime >= this.VertexTxOptions.TxSecondsTimeout;
        protected async Task<bool> Commit(string txId = default)
        {
            if (TxSnapshot.TxId != txId)
            {
                throw new TxException($"{TxSnapshot.TxId}!={txId}");
            }
            if (TxSnapshot.Status != Abstractions.TransactionStatus.WaitingCommit)
                throw new TxException("TransactionStatus must be 'WaitingCommit'");
            if (this.WaitingCommitEventList.Count > 0)
            {
                try
                {
                    await this.OnTxCommit(txId);
                    foreach (var transport in this.WaitingCommitEventList)
                    {
                        await this.OnRaiseStart(transport.EventUnit);
                    }
                    await this.EventStorage.TxAppend(this.WaitingCommitEventList.Select(o => o.Document).ToList());
                    await this.OnTxCommited(txId);
                    TxSnapshot.Status = Abstractions.TransactionStatus.Commited;
                    if (this.Logger.IsEnabled(LogLevel.Trace))
                    {
                        this.Logger.LogTrace("Transaction Commited: {0}->{1}->{2}", this.ActorType.FullName, this.ActorId.ToString(), txId);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    this.Logger.LogCritical(ex, "Transaction failed: {0}->{1}->{2}", this.ActorType.FullName, this.ActorId.ToString(), txId);
                    throw;
                }
            }
            return false;
        }

        protected async Task Finish(string txId = default)
        {
            if (TxSnapshot.TxId == txId)
            {
                if (TxSnapshot.Status != Abstractions.TransactionStatus.Commited)
                    throw new TxException("TransactionStatus must be 'Commited'");
                if (this.WaitingCommitEventList.Count > 0)
                {
                    await this.OnTxFinsh(txId);
                    //If the copy snapshot is not updated, update the copy set
                    foreach (var transport in this.WaitingCommitEventList)
                    {
                        await this.OnRaiseSuccess(transport.EventUnit, transport.EventBytes);
                    }

                    await this.SaveSnapshotAsync();

                    if (this.EventStream != default)
                    {
                        try
                        {
                            foreach (var transport in this.WaitingCommitEventList)
                            {
                                await this.EventStream.Next(transport.GetEventTransSpan().ToArray());
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, ex.Message);
                        }
                    }

                    this.WaitingCommitEventList.ForEach(transport => transport.Dispose());
                    this.WaitingCommitEventList.Clear();
                }
                if (TxSnapshot.Status != Abstractions.TransactionStatus.None)
                    this.TxSnapshot.Reset();
                this.TxBeginLock.Release();
            }
        }

        protected async Task Rollback(string txId = default)
        {
            if (TxSnapshot.TxId == txId)
            {
                try
                {
                    if (this.Snapshot.Meta.Version >= TxSnapshot.TxStartVersion)
                    {
                        var oldSnapshot = TxSnapshot with { };
                        await this.OnTxRollback(txId);
                        if (oldSnapshot.Status == Abstractions.TransactionStatus.Commited)
                        {
                            await this.EventStorage.DeleteAfter(this.Snapshot.Meta.ActorId, oldSnapshot.TxStartVersion);
                        }
                        if (this.BackupSnapshot.Meta.Version == oldSnapshot.TxStartVersion - 1)
                        {
                            this.Snapshot = new SnapshotUnit<PrimaryKey, T>
                            {
                                Meta = this.BackupSnapshot.Meta with { },
                                Data = this.BackupSnapshot.Data.Clone(this.Serializer)
                            };
                        }
                        else
                        {
                            await this.RecoverySnapshot();
                        }

                        this.WaitingCommitEventList.Clear();
                        if (this.Logger.IsEnabled(LogLevel.Trace))
                        {
                            this.Logger.LogTrace("Tx rollback successed: {0}->{1}->{2}", this.ActorType.FullName, this.ActorId.ToString(), txId);
                        }
                    }
                    if (TxSnapshot.Status != Abstractions.TransactionStatus.None)
                        this.TxSnapshot.Reset();
                    this.TxBeginLock.Release();
                }
                catch (Exception ex)
                {
                    this.Logger.LogCritical(ex, "Tx rollback failed: {0}->{1}->{2}", this.ActorType.FullName, this.ActorId.ToString(), txId);
                    throw;
                }
            }
        }
        /// <summary>
        /// Prevent objects from interfering with each other in Snapshot and BackupSnapshot, so deserialize a brand new Event object to BackupSnapshot
        /// </summary>
        /// <param name="eventUnit">Event body</param>
        /// <param name="bitUnit"></param>
        /// <returns></returns>
        protected override ValueTask OnRaiseSuccess(EventUnit<PrimaryKey> eventUnit, byte[] eventBytes)
        {
            if (this.BackupSnapshot.Meta.Version + 1 == eventUnit.Meta.Version)
            {
                var copiedEvent = new EventUnit<PrimaryKey>
                {
                    ActorId = this.ActorId,
                    Event = this.Serializer.Deserialize(eventBytes, eventUnit.Event.GetType()) as IEvent,
                    Meta = eventUnit.Meta with { }
                };
                this.SnapshotHandler.Apply(this.BackupSnapshot, copiedEvent);
                this.BackupSnapshot.Meta.ForceUpdateVersion(copiedEvent.Meta, this.ActorType);//Version of the update process
            }
            //The parent is involved in the state archive
            return base.OnRaiseSuccess(eventUnit, eventBytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SnapshotCheck()
        {
            if (this.BackupSnapshot.Meta.Version != this.Snapshot.Meta.Version)
            {
                throw new TxSnapshotException(this.Snapshot.Meta.ActorId.ToString(), this.Snapshot.Meta.Version, this.BackupSnapshot.Meta.Version);
            }
        }
        /// <summary>
        /// Transactional event submission
        /// The transaction must be opened before using this function, otherwise an exception will occur
        /// </summary>
        /// <param name="event"></param>
        /// <param name="uniqueKey"></param>
        ///  <param name="txId"></param>
        protected virtual ValueTask TxRaiseEvent(IEvent @event, string flowId = default, string txId = default)
        {
            if (string.IsNullOrEmpty(flowId))
            {
                flowId = RequestContext.Get(ActorConsts.eventFlowIdKey) as string;
                if (string.IsNullOrEmpty(flowId))
                    throw new ArgumentNullException(nameof(flowId));
            }
            try
            {
                if (TxSnapshot.TxId != txId)
                {
                    throw new TxException($"Transaction {txId} not opened");
                }

                this.Snapshot.Meta.IncrementDoingVersion(this.ActorType);//Mark the Version to be processed
                var fullyEvent = new EventUnit<PrimaryKey>
                {
                    ActorId = this.ActorId,
                    Event = @event,
                    Meta = new EventMeta
                    {
                        FlowId = flowId,
                        Version = this.Snapshot.Meta.Version + 1,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    }
                };
                this.WaitingCommitEventList.Add(new EventBufferUnit<PrimaryKey>(fullyEvent, flowId, EventTypeContainer, Serializer));
                this.SnapshotHandler.Apply(this.Snapshot, fullyEvent);
                this.Snapshot.Meta.UpdateVersion(fullyEvent.Meta, this.ActorType);//Version of the update process
                if (this.Logger.IsEnabled(LogLevel.Trace))
                {
                    this.Logger.LogTrace("TxRaiseEvent completed: {0}->{1}->{2}", this.ActorType.FullName, this.Serializer.Serialize(fullyEvent), this.Serializer.Serialize(this.Snapshot));
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, "TxRaiseEvent failed: {0}->{1}->{2}", this.ActorType.FullName, this.Serializer.Serialize(@event, @event.GetType()), this.Serializer.Serialize(this.Snapshot));
                this.Snapshot.Meta.DecrementDoingVersion();//Restore the doing version
                throw;
            }
            return ValueTask.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnTxCommit(string transactionId) => ValueTask.CompletedTask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnTxCommited(string transactionId) => ValueTask.CompletedTask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnTxFinsh(string transactionId) => ValueTask.CompletedTask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnTxRollback(string transactionId) => ValueTask.CompletedTask;
    }
}
