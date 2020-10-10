using IdGen;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vertex.Runtime.Actor;
using Vertex.Transaction.Abstractions;
using Vertex.Transaction.Abstractions.IActor;
using Vertex.Transaction.Events.TxUnit;
using Vertex.Transaction.Snapshot;

namespace Vertex.Transaction.Actor
{
    public abstract class DTxUnitActor<PrimaryKey, TRequest, TResponse> :
        ReentryActor<PrimaryKey, TxUnitSnapshot<TRequest>>,
        IDTxUnitActor<TRequest, TResponse>
        where TRequest : class, new()
    {
        private readonly IdGenerator idGen = new IdGenerator(0);
        private static AsyncLocal<DTxCommit<TRequest>> currentCommit = new AsyncLocal<DTxCommit<TRequest>>();
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            this.RegisterTimer(
                async state =>
                {
                    foreach (var commit in this.Snapshot.Data.RequestDict.Values.Where(o => (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - o.Timestamp) >= 60).ToList())
                    {
                        var actors = this.EffectActors(commit.Data);
                        currentCommit.Value = commit;
                        if (commit.Status == TransactionStatus.WaitingCommit)
                        {
                            await Rollback(actors);
                        }
                        else if (commit.Status == TransactionStatus.Commited)
                        {
                            await Finish(actors);
                        }
                    }
                }, null,
                new TimeSpan(0, 0, 5),
                new TimeSpan(0, 0, 30));
        }

        protected async Task Commit()
        {
            var actors = this.EffectActors(currentCommit.Value.Data);
            var commit = currentCommit.Value;
            try
            {
                await ConcurrentRaiseEvent(new UnitCommitEvent { TxId = commit.TxId, Data = Serializer.Serialize(commit), StartTime = commit.Timestamp });
                await Commit(actors);
                await this.Finish(actors);
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, ex.Message);
                throw;
            }
        }

        protected Task Rollback()
        {
            return this.Rollback(this.EffectActors(currentCommit.Value.Data));
        }
        public async Task<TResponse> Ask(TRequest request)
        {
            currentCommit.Value = new DTxCommit<TRequest>
            {
                TxId = $"{this.ActorType.Name}_{this.ActorId}_{idGen.CreateId()}",
                Status = TransactionStatus.None,
                Data = request,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            RequestContext.Set(RequestContextKeys.txIdKey, currentCommit.Value.TxId);
            RequestContext.Set(ActorConsts.eventFlowIdKey, FlowId(request));
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
            var commit = currentCommit.Value;
            if (commit.Status != TransactionStatus.Successed && commit.Status != TransactionStatus.Commited)
            {
                RequestContext.Set(RequestContextKeys.txIdKey, commit.TxId);
                await Task.WhenAll(actors.Select(a => a.Rollback()));
                if (this.Snapshot.Data.RequestDict.ContainsKey(commit.TxId))
                {
                    await ConcurrentRaiseEvent(new UnitFinishedEvent { TxId = commit.TxId, Status = TransactionStatus.Rollbacked });
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
            var commit = currentCommit.Value;
            RequestContext.Set(RequestContextKeys.txIdKey, commit.TxId);
            var results = await Task.WhenAll(actors.Select(a => a.Commit()));
            if (!results.All(o => o))
                throw new System.Transactions.TransactionException(nameof(commit));
            await ConcurrentRaiseEvent(new UnitCommitedEvent { TxId = commit.TxId });
        }
        private async Task Finish(IDTxActor[] actors)
        {
            if (actors is null || actors.Length == 0)
            {
                throw new NotImplementedException(nameof(this.EffectActors));
            }
            var commit = currentCommit.Value;
            RequestContext.Set(RequestContextKeys.txIdKey, commit.TxId);
            await Task.WhenAll(actors.Select(a => a.Finish()));
            await ConcurrentRaiseEvent(new UnitFinishedEvent { TxId = commit.TxId, Status = TransactionStatus.Successed });
        }
    }
}
