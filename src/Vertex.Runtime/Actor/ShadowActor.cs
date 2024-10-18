using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Runtime;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Serialization;
using Vertex.Abstractions.Snapshot;
using Vertex.Abstractions.Storage;
using Vertex.Runtime.Exceptions;
using Vertex.Runtime.Options;

namespace Vertex.Runtime.Actor
{
    public abstract class ShadowActor<TPrimaryKey, T> : ActorBase<TPrimaryKey>, IFlowActor
        where T : ISnapshot, new()
    {
        #region property
        protected ShadowActorOptions VertexOptions { get; private set; }

        public abstract IVertexActor Vertex { get; set; }

        protected ILogger Logger { get; private set; }

        protected ISerializer Serializer { get; private set; }

        protected IEventTypeContainer EventTypeContainer { get; private set; }

        /// <summary>
        /// Memory state, restored by snapshot + Event play or replay
        /// </summary>
        protected SnapshotUnit<TPrimaryKey, T> Snapshot { get; set; }

        protected ISnapshotHandler<TPrimaryKey, T> SnapshotHandler { get; private set; }

        /// <summary>
        /// The event version number of the snapshot
        /// </summary>
        protected long ActivateSnapshotVersion { get; private set; }

        public ISnapshotStorage<TPrimaryKey> SnapshotStorage { get; private set; }

        #endregion
        #region Activate

        /// <summary>
        /// Unified method of dependency injection
        /// </summary>
        /// <returns></returns>
        protected virtual async ValueTask DependencyInjection()
        {
            this.VertexOptions = this.ServiceProvider.GetService<IOptionsMonitor<ShadowActorOptions>>().Get(this.ActorType.FullName);
            this.Serializer = this.ServiceProvider.GetService<ISerializer>();
            this.EventTypeContainer = this.ServiceProvider.GetService<IEventTypeContainer>();
            this.Logger = (ILogger)this.ServiceProvider.GetService(typeof(ILogger<>).MakeGenericType(this.ActorType));

            var snapshotStorageFactory = this.ServiceProvider.GetService<ISnapshotStorageFactory>();
            this.SnapshotStorage = await snapshotStorageFactory.Create(this);
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            await this.DependencyInjection();

            try
            {
                // Load snapshot
                await this.RecoverySnapshot();

                if (this.Logger.IsEnabled(LogLevel.Trace))
                {
                    this.Logger.LogTrace("Activation completed: {0}->{1}", this.ActorType.FullName, this.Serializer.Serialize(this.Snapshot));
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, "Activation failed: {0}->{1}", this.ActorType.FullName, this.ActorId.ToString());
                throw;
            }
        }

        protected virtual async Task RecoverySnapshot()
        {
            try
            {
                await this.ReadSnapshotAsync();
                while (!this.Snapshot.Meta.IsLatest)
                {
                    var documentList = await this.Vertex.GetEventDocuments(this.Snapshot.Meta.Version + 1, this.Snapshot.Meta.Version + this.VertexOptions.EventPageSize);
                    foreach (var document in documentList)
                    {
                        if (!this.EventTypeContainer.TryGet(document.Name, out var type))
                        {
                            throw new NoNullAllowedException($"event name of {document.Name}");
                        }
                        var data = this.Serializer.Deserialize(document.Data, type);
                        var evt = new EventUnit<TPrimaryKey>
                        {
                            ActorId = this.ActorId,
                            Event = data as IEvent,
                            Meta = new EventMeta { Version = document.Version, Timestamp = document.Timestamp, FlowId = document.FlowId }
                        };
                        this.Snapshot.Meta.IncrementDoingVersion(this.ActorType); // Mark the Version to be processed
                        this.SnapshotHandler.Apply(this.Snapshot, evt);
                        this.Snapshot.Meta.UpdateVersion(evt.Meta, this.ActorType); // Version of the update process
                    }

                    if (documentList.Count < this.VertexOptions.EventPageSize)
                    {
                        break;
                    }
                }

                if (this.Logger.IsEnabled(LogLevel.Trace))
                {
                    this.Logger.LogTrace("Recovery completed: {0}->{1}", this.ActorType.FullName, this.Serializer.Serialize(this.Snapshot));
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, "Recovery failed: {0}->{1}", this.ActorType.FullName, this.ActorId.ToString());
                throw;
            }
        }

        protected virtual async Task ReadSnapshotAsync()
        {
            try
            {
                // Restore state from snapshot
                this.Snapshot = await this.SnapshotStorage.Get<T>(this.ActorId);
                if (this.Snapshot is null)
                {
                    // New status
                    await this.CreateSnapshot();
                }
                this.ActivateSnapshotVersion = this.Snapshot.Meta.Version;
                if (this.Logger.IsEnabled(LogLevel.Information))
                {
                    this.Logger.LogTrace("ReadSnapshot completed: {0}->{1}", this.ActorType.FullName, this.Serializer.Serialize(this.Snapshot));
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, "ReadSnapshot failed: {0}->{1}", this.ActorType.FullName, this.ActorId.ToString());
                throw;
            }
        }

        /// <summary>
        /// Initialization state, must be implemented
        /// </summary>
        /// <returns></returns>
        protected virtual ValueTask CreateSnapshot()
        {
            this.Snapshot = new SnapshotUnit<TPrimaryKey, T> { Meta = new SnapshotMeta<TPrimaryKey> { ActorId = this.ActorId }, Data = new T() };
            return ValueTask.CompletedTask;
        }
        #endregion

        public Task OnNext(Immutable<byte[]> bytes)
        {
            throw new NotImplementedException();
        }

        public Task OnNext(Immutable<List<byte[]>> items)
        {
            throw new NotImplementedException();
        }

        protected async ValueTask Tell(EventUnit<TPrimaryKey> eventUnit)
        {
            if (eventUnit.Meta.Version == this.Snapshot.Meta.Version + 1)
            {
                await this.EventDelivered(eventUnit);

                this.Snapshot.Meta.ForceUpdateVersion(eventUnit.Meta, this.ActorType); // 更新处理完成的Version
            }
            else if (eventUnit.Meta.Version > this.Snapshot.Meta.Version)
            {
                var documentList = await this.Vertex.GetEventDocuments(this.Snapshot.Meta.Version + 1, eventUnit.Meta.Version - 1);
                var evtList = documentList.Select(document =>
                {
                    if (!this.EventTypeContainer.TryGet(document.Name, out var type))
                    {
                        throw new NoNullAllowedException($"event name of {document.Name}");
                    }
                    var data = this.Serializer.Deserialize(document.Data, type);
                    return new EventUnit<TPrimaryKey>
                    {
                        ActorId = this.ActorId,
                        Event = data as IEvent,
                        Meta = new EventMeta { Version = document.Version, Timestamp = document.Timestamp, FlowId = document.FlowId }
                    };
                });
                foreach (var evt in evtList)
                {
                    await this.EventDelivered(evt);

                    this.Snapshot.Meta.ForceUpdateVersion(evt.Meta, this.ActorType); // 更新处理完成的Version
                }
            }

            if (eventUnit.Meta.Version == this.Snapshot.Meta.Version + 1)
            {
                await this.EventDelivered(eventUnit);

                this.Snapshot.Meta.ForceUpdateVersion(eventUnit.Meta, this.ActorType); // 更新处理完成的Version
            }

            if (eventUnit.Meta.Version > this.Snapshot.Meta.Version)
            {
                throw new EventVersionException(this.ActorId.ToString(), this.ActorType, eventUnit.Meta.Version, this.Snapshot.Meta.Version);
            }
        }

        private async ValueTask EventDelivered(EventUnit<TPrimaryKey> eventUnit)
        {
            try
            {
                RequestContext.Set(RuntimeConsts.EventFlowIdKey, eventUnit.Meta.FlowId);
                this.SnapshotHandler.Apply(this.Snapshot, eventUnit);
                await this.OnEventDelivered(eventUnit);
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, "Delivered failed: {0}->{1}->{2}", this.ActorType.FullName, this.ActorId.ToString(), this.Serializer.Serialize(eventUnit, eventUnit.Event.GetType()));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnEventDelivered(EventUnit<TPrimaryKey> eventUnit) => ValueTask.CompletedTask;
    }
}
