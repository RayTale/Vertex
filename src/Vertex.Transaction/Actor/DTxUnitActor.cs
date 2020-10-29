using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdGen;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Vertex.Runtime;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Abstractions.IActor;
using Vertex.Transaction.Events.TxUnit;
using Vertex.Transaction.Snapshot;

namespace Vertex.Transaction.Actor
{
    public abstract class DTxUnitActor<TPrimaryKey, TRequest, TResponse> :
        ReentryActor<TPrimaryKey, TxUnitSnapshot<TRequest>>,
        IDTxUnitActor<TRequest, TResponse>,
        IRemindable
        where TRequest : class, new()
    {
        private const string ReminderName = "monitor";
        private static readonly AsyncLocal<DTxCommit<TRequest>> CurrentCommit = new AsyncLocal<DTxCommit<TRequest>>();
        private readonly IdGenerator idGen = new IdGenerator(0);
        private IDisposable timer;

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            this.timer = this.RegisterTimer(
                async state =>
                {
                    foreach (var commit in this.Snapshot.Data.RequestDict.Values.Where(o => (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - o.Timestamp) >= 60).ToList())
                    {
                        try
                        {
                            var actors = this.EffectActors(commit.Data);
                            CurrentCommit.Value = commit;
                            if (commit.Status == TransactionStatus.WaitingCommit)
                            {
                                await this.Rollback(actors);
                            }
                            else if (commit.Status == TransactionStatus.Commited)
                            {
                                await this.Finish(actors);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogCritical(ex, this.Serializer.Serialize(commit));
                        }
                    }
                }, null,
                new TimeSpan(0, 0, 5),
                new TimeSpan(0, 0, 30));
        }

        public override async Task OnDeactivateAsync()
        {
            if (this.Snapshot.Data.RequestDict.Count > 0)
            {
                await this.RegisterOrUpdateReminder(ReminderName, new TimeSpan(0, 3, 0), new TimeSpan(0, 5, 0));
            }
            else
            {
                var reminder = await this.GetReminder(ReminderName);
                if (reminder != default)
                {
                    await this.UnregisterReminder(reminder);
                }
            }
            this.timer.Dispose();
            await base.OnDeactivateAsync();
        }

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            return Task.CompletedTask;
        }

        protected async Task Commit()
        {
            var actors = this.EffectActors(CurrentCommit.Value.Data);
            var commit = CurrentCommit.Value;
            try
            {
                await this.ConcurrentRaiseEvent(new UnitCommitEvent { TxId = commit.TxId, Data = this.Serializer.Serialize(commit), StartTime = commit.Timestamp });
                await this.Commit(actors);
                await this.Finish(actors);
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, this.Serializer.Serialize(commit));
                throw;
            }
        }

        protected Task Rollback()
        {
            return this.Rollback(this.EffectActors(CurrentCommit.Value.Data));
        }

        public async Task<TResponse> Ask(TRequest request)
        {
            CurrentCommit.Value = new DTxCommit<TRequest>
            {
                TxId = $"{this.ActorType.Name}_{this.ActorId}_{this.idGen.CreateId()}",
                Status = TransactionStatus.None,
                Data = request,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            RequestContext.Set(RequestContextKeys.TxIdKey, CurrentCommit.Value.TxId);
            RequestContext.Set(RuntimeConsts.EventFlowIdKey, this.FlowId(request));
            return await this.Work(request);
        }

        public abstract string FlowId(TRequest request);

        public abstract Task<TResponse> Work(TRequest request);

        protected abstract IDTxActor[] EffectActors(TRequest request);

        private async Task Rollback(IDTxActor[] actors)
        {
            if (actors is null || actors.Length == 0)
            {
                throw new NotImplementedException(nameof(this.EffectActors));
            }
            var commit = CurrentCommit.Value;
            if (commit.Status != TransactionStatus.Successed && commit.Status != TransactionStatus.Commited)
            {
                RequestContext.Set(RequestContextKeys.TxIdKey, commit.TxId);
                await Task.WhenAll(actors.Select(a => a.Rollback()));
                if (this.Snapshot.Data.RequestDict.ContainsKey(commit.TxId))
                {
                    await this.ConcurrentRaiseEvent(new UnitFinishedEvent { TxId = commit.TxId, Status = TransactionStatus.Rollbacked });
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(commit.Status.ToString());
            }
        }

        private async Task Commit(IDTxActor[] actors)
        {
            if (actors is null || actors.Length == 0)
            {
                throw new NotImplementedException(nameof(this.EffectActors));
            }
            var commit = CurrentCommit.Value;
            RequestContext.Set(RequestContextKeys.TxIdKey, commit.TxId);
            var results = await Task.WhenAll(actors.Select(a => a.Commit()));
            if (!results.All(o => o))
            {
                throw new System.Transactions.TransactionException(nameof(commit));
            }

            await this.ConcurrentRaiseEvent(new UnitCommitedEvent { TxId = commit.TxId });
        }

        private async Task Finish(IDTxActor[] actors)
        {
            if (actors is null || actors.Length == 0)
            {
                throw new NotImplementedException(nameof(this.EffectActors));
            }
            var commit = CurrentCommit.Value;
            RequestContext.Set(RequestContextKeys.TxIdKey, commit.TxId);
            await Task.WhenAll(actors.Select(a => a.Finish()));
            await this.ConcurrentRaiseEvent(new UnitFinishedEvent { TxId = commit.TxId, Status = TransactionStatus.Successed });
        }
    }
}
