using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.EventStream;
using Vertex.Abstractions.Serialization;
using Vertex.Abstractions.Snapshot;
using Vertex.Abstractions.Storage;
using Vertex.Protocol;
using Vertex.Runtime.Event;
using Vertex.Runtime.Exceptions;
using Vertex.Runtime.Options;
using Vertext.Abstractions.Event;

namespace Vertex.Runtime.Actor
{
    public class VertexActor<TPrimaryKey, T> : ActorBase<TPrimaryKey>, IVertexActor
         where T : ISnapshot, new()
    {
        /// <summary>
        /// The event version number of the snapshot
        /// </summary>
        protected long ActivateSnapshotVersion { get; private set; }

        protected ActorOptions VertexOptions { get; private set; }

        protected ArchiveOptions ArchiveOptions { get; private set; }

        protected ILogger Logger { get; private set; }

        protected IEventStream EventStream { get; private set; }

        protected ISerializer Serializer { get; private set; }

        protected IEventTypeContainer EventTypeContainer { get; private set; }

        protected SnapshotUnit<TPrimaryKey, T> Snapshot { get; set; }

        protected ISnapshotHandler<TPrimaryKey, T> SnapshotHandler { get; private set; }

        /// <summary>
        /// Archive storage
        /// </summary>
        protected IEventArchive<TPrimaryKey> EventArchive { get; private set; }

        /// <summary>
        /// Event storage
        /// </summary>
        protected IEventStorage<TPrimaryKey> EventStorage { get; private set; }

        /// <summary>
        /// State storage
        /// </summary>
        protected ISnapshotStorage<TPrimaryKey> SnapshotStorage { get; private set; }

        /// <summary>
        /// Unified method of dependency injection
        /// </summary>
        /// <returns></returns>
        protected virtual async ValueTask DependencyInjection()
        {
            this.VertexOptions = this.ServiceProvider.GetService<IOptionsMonitor<ActorOptions>>().Get(this.ActorType.FullName);
            this.ArchiveOptions = this.ServiceProvider.GetService<IOptionsMonitor<ArchiveOptions>>().Get(this.ActorType.FullName);
            this.Logger = (ILogger)this.ServiceProvider.GetService(typeof(ILogger<>).MakeGenericType(this.ActorType));
            this.Serializer = this.ServiceProvider.GetService<ISerializer>();
            this.EventTypeContainer = this.ServiceProvider.GetService<IEventTypeContainer>();
            this.SnapshotHandler = this.ServiceProvider.GetService<ISnapshotHandler<TPrimaryKey, T>>();
            if (this.SnapshotHandler == default)
            {
                throw new VertexEventHandlerException(this.ActorType);
            }
            var eventStreamFactory = this.ServiceProvider.GetService<IEventStreamFactory>();
            this.EventStream = await eventStreamFactory.Create(this);
            var eventStorageFactory = this.ServiceProvider.GetService<IEventStorageFactory>();
            this.EventStorage = await eventStorageFactory.Create(this);
            var eventArchiveFactory = this.ServiceProvider.GetService<IEventArchiveFactory>();
            this.EventArchive = await eventArchiveFactory.Create(this);
            var snapshotStorageFactory = this.ServiceProvider.GetService<ISnapshotStorageFactory>();
            this.SnapshotStorage = await snapshotStorageFactory.Create(this);
        }

        /// <summary>
        /// The method used to initialize is called when Grain is activated (overriding in subclasses is prohibited)
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            await this.DependencyInjection();

            // Load snapshot
            await this.RecoverySnapshot();
            try
            {
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
                    var documents = await this.EventStorage.GetList(this.ActorId, this.Snapshot.Meta.Version + 1, this.Snapshot.Meta.Version + this.VertexOptions.EventPageSize);
                    var eventList = this.Convert(documents);
                    foreach (var fullyEvent in eventList)
                    {
                        this.Snapshot.Meta.IncrementDoingVersion(this.ActorType); // Mark the Version to be processed
                        this.SnapshotHandler.Apply(this.Snapshot, fullyEvent);
                        this.Snapshot.Meta.UpdateVersion(fullyEvent.Meta, this.ActorType); // Version of the update process
                    }

                    if (eventList.Count < this.VertexOptions.EventPageSize)
                    {
                        break;
                    }
                }

                // If the minimum snapshot save interval version number is met, the snapshot is saved once
                if (this.Snapshot.Meta.Version - this.ActivateSnapshotVersion >= this.VertexOptions.MinSnapshotVersionInterval)
                {
                    await this.SaveSnapshotAsync(true, true);
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

        protected async ValueTask SaveSnapshotAsync(bool force = false, bool isLatest = false)
        {
            if (this.Snapshot.Meta.Version != this.Snapshot.Meta.DoingVersion)
            {
                throw new SnapshotException(this.Snapshot.Meta.ActorId.ToString(), this.ActorType, this.Snapshot.Meta.DoingVersion, this.Snapshot.Meta.Version);
            }

            // Update the snapshot if the version number difference exceeds the setting
            if ((force && (this.Snapshot.Meta.Version > this.ActivateSnapshotVersion || this.Snapshot.Meta.IsLatest != isLatest)) ||
                (this.Snapshot.Meta.Version - this.ActivateSnapshotVersion >= this.VertexOptions.SnapshotVersionInterval))
            {
                try
                {
                    await this.OnStartSaveSnapshot();
                    this.Snapshot.Meta.IsLatest = isLatest;
                    if (this.ActivateSnapshotVersion == 0)
                    {
                        await this.SnapshotStorage.Insert(this.Snapshot);
                    }
                    else
                    {
                        await this.SnapshotStorage.Update(this.Snapshot);
                    }

                    this.ActivateSnapshotVersion = this.Snapshot.Meta.Version;
                    if (this.Logger.IsEnabled(LogLevel.Trace))
                    {
                        this.Logger.LogTrace("SaveSnapshot completed: {0}->{1}", this.ActorType.FullName, this.Serializer.Serialize(this.Snapshot));
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogCritical(ex, "SaveSnapshot failed: {0}->{1}", this.ActorType.FullName, this.ActorId.ToString());
                    throw;
                }
            }
        }

        /// <summary>
        /// Initialization state, must be implemented
        /// </summary>
        /// <returns></returns>
        protected virtual ValueTask CreateSnapshot()
        {
            this.Snapshot = new SnapshotUnit<TPrimaryKey, T>
            {
                Meta = new SnapshotMeta<TPrimaryKey>
                {
                    ActorId = this.ActorId,
                    MinEventTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                },
                Data = new T()
            };
            return ValueTask.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnActivationCompleted() => ValueTask.CompletedTask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnStartSaveSnapshot() => ValueTask.CompletedTask;

        public override async Task OnDeactivateAsync()
        {
            try
            {
                if (this.Snapshot.Meta.Version - this.ActivateSnapshotVersion >= this.VertexOptions.MinSnapshotVersionInterval)
                {
                    await this.SaveSnapshotAsync(true, true);
                    await this.OnDeactivated();
                }
                await this.Archive();
                if (this.Logger.IsEnabled(LogLevel.Trace))
                {
                    this.Logger.LogTrace("Deactivate completed: {0}->{1}", this.ActorType.FullName, this.Serializer.Serialize(this.Snapshot));
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, "Deactivate failed: {0}->{1}", this.ActorType.FullName, this.ActorId.ToString());
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnDeactivated() => ValueTask.CompletedTask;

        #region RaiseEvent
        protected virtual async Task<bool> RaiseEvent(IEvent @event, string flowId = null)
        {
            if (string.IsNullOrEmpty(flowId))
            {
                flowId = RequestContext.Get(RuntimeConsts.EventFlowIdKey) as string;
                if (string.IsNullOrEmpty(flowId))
                {
                    throw new ArgumentNullException(nameof(flowId));
                }
            }
            try
            {
                var eventBox = new EventUnit<TPrimaryKey>
                {
                    Event = @event,
                    Meta = new EventMeta
                    {
                        FlowId = flowId,
                        Version = this.Snapshot.Meta.Version + 1,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    },
                    ActorId = this.Snapshot.Meta.ActorId
                };

                await this.OnRaiseStart(eventBox);

                this.Snapshot.Meta.IncrementDoingVersion(this.ActorType); // Mark the Version to be processed
                var evtType = @event.GetType();

                if (!this.EventTypeContainer.TryGet(evtType, out var eventName))
                {
                    throw new NoNullAllowedException($"event name of {evtType.FullName}");
                }

                var evtBytes = this.Serializer.SerializeToUtf8Bytes(@event, evtType);
                var appendResult = await this.EventStorage.Append(new EventDocument<TPrimaryKey>
                {
                    FlowId = eventBox.Meta.FlowId,
                    ActorId = this.ActorId,
                    Data = Encoding.UTF8.GetString(evtBytes),
                    Name = eventName,
                    Timestamp = eventBox.Meta.Timestamp,
                    Version = eventBox.Meta.Version
                });

                if (appendResult)
                {
                    this.SnapshotHandler.Apply(this.Snapshot, eventBox);
                    this.Snapshot.Meta.UpdateVersion(eventBox.Meta, this.ActorType); // Version of the update process
                    await this.OnRaiseSuccess(eventBox, evtBytes);
                    await this.SaveSnapshotAsync();
                    using var baseBytes = eventBox.Meta.ConvertToBytes();
                    using var buffer = EventConverter.ConvertToBytes(new EventTransUnit(eventName, this.Snapshot.Meta.ActorId, baseBytes.AsSpan(), evtBytes));
                    if (this.EventStream != default)
                    {
                        await this.EventStream.Next(buffer.ToArray());
                    }

                    if (this.Logger.IsEnabled(LogLevel.Trace))
                    {
                        this.Logger.LogTrace("RaiseEvent completed: {0}->{1}->{2}", this.ActorType.FullName, this.Serializer.Serialize(eventBox), this.Serializer.Serialize(this.Snapshot));
                    }

                    return true;
                }
                else
                {
                    if (this.Logger.IsEnabled(LogLevel.Trace))
                    {
                        this.Logger.LogTrace("RaiseEvent failed: {0}->{1}->{2}", this.ActorType.FullName, this.Serializer.Serialize(eventBox), this.Serializer.Serialize(this.Snapshot));
                    }

                    this.Snapshot.Meta.DecrementDoingVersion(); // Restore the doing version
                    await this.OnRaiseFailed(eventBox);

                    return false;
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, "RaiseEvent failed: {0}->{1}", this.ActorType.FullName, this.Serializer.Serialize(this.Snapshot));
                await this.RecoverySnapshot(); // Restore state

                // Errors may appear repeatedly, so update the previous snapshot to improve the restoration speed
                await this.SaveSnapshotAsync(true);

                throw;
            }
        }

        protected virtual async ValueTask OnRaiseStart(EventUnit<TPrimaryKey> @event)
        {
            if (this.Snapshot.Meta.Version == 0)
            {
                return;
            }

            if (this.Snapshot.Meta.IsLatest)
            {
                await this.SnapshotStorage.UpdateIsLatest(this.Snapshot.Meta.ActorId, false);
                this.Snapshot.Meta.IsLatest = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnRaiseSuccess(EventUnit<TPrimaryKey> eventUnit, byte[] eventBits) => this.Archive();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnRaiseFailed(EventUnit<TPrimaryKey> @event) => ValueTask.CompletedTask;
        #endregion

        protected ValueTask Archive()
        {
            if (this.Snapshot.Meta.Version != this.Snapshot.Meta.DoingVersion)
            {
                throw new SnapshotException(this.Snapshot.Meta.ActorId.ToString(), this.ActorType, this.Snapshot.Meta.DoingVersion, this.Snapshot.Meta.Version);
            }
            var (can, endStartTime) = this.CanArchive();
            if (can)
            {
                return new ValueTask(this.Archive(endStartTime));
            }
            return ValueTask.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected (bool can, long endTimestamp) CanArchive()
        {
            if (this.Snapshot.Meta.Version > this.Snapshot.Meta.MinEventVersion)
            {
                var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var endTimestamp = nowTimestamp - this.ArchiveOptions.RetainSeconds;
                if (endTimestamp >= (this.Snapshot.Meta.MinEventTimestamp + this.ArchiveOptions.MinIntervalSeconds))
                {
                    return (true, endTimestamp);
                }
            }
            return (false, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected async Task Archive(long endTimestamp)
        {
            var takes = 0;
            while (true)
            {
                var events = await this.EventStorage.GetList(this.ActorId, endTimestamp, takes, this.ArchiveOptions.EventPageSize);
                if (events.Count > 0)
                {
                    await this.EventArchive.Arichive(events);
                    this.Snapshot.Meta.MinEventTimestamp = events.Max(o => o.Timestamp);
                    this.Snapshot.Meta.MinEventVersion += events.Count;
                }
                if (events.Count < this.ArchiveOptions.EventPageSize)
                {
                    break;
                }

                takes += events.Count;
            }
            this.Snapshot.Meta.MinEventTimestamp = endTimestamp;
            var isLatest = this.Snapshot.Meta.Version - this.Snapshot.Meta.MinEventVersion == -1;
            await this.SaveSnapshotAsync(true, isLatest);

            await this.EventStorage.DeletePrevious(this.ActorId, this.Snapshot.Meta.MinEventVersion);
        }

        public async Task<IList<EventDocumentDto>> GetEventDocuments(long startVersion, long endVersion)
        {
            var results = endVersion >= this.Snapshot.Meta.MinEventVersion ? await this.EventStorage.GetList(this.ActorId, startVersion, endVersion) : new List<EventDocument<TPrimaryKey>>();
            var capacity = endVersion - startVersion + 1;
            if (results.Count < capacity && (results.Count == 0 || results.Min(r => r.Version) != 1))
            {
                if (results.Count > 0)
                {
                    endVersion = results.Min(o => o.Version) - 1;
                }
                var archiveEvents = await this.EventArchive.GetList(this.ActorId, startVersion, endVersion);
                if (archiveEvents.Count > 0)
                {
                    results.AddRange(archiveEvents);
                    results = results.OrderBy(r => r.Version).ToList();
                }
            }
            return results.Select(o => new EventDocumentDto
            {
                Data = o.Data,
                Name = o.Name,
                FlowId = o.FlowId,
                Version = o.Version,
                Timestamp = o.Timestamp
            }).ToList();
        }

        protected IList<EventUnit<TPrimaryKey>> Convert(IList<EventDocument<TPrimaryKey>> documents)
        {
            return documents.Select(document =>
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
            }).ToList();
        }
    }
}
