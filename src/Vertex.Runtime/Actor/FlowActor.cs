using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Vertex.Abstractions.Actor;
using Vertex.Abstractions.Attributes;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Serialization;
using Vertex.Abstractions.Snapshot;
using Vertex.Abstractions.Storage;
using Vertex.Protocol;
using Vertex.Runtime.Actor.Attributes;
using Vertex.Runtime.Event;
using Vertex.Runtime.Exceptions;
using Vertex.Runtime.Options;
using Vertex.Utils.Emit;
using Vertext.Abstractions.Event;

namespace Vertex.Runtime.Actor
{
    public abstract class FlowActor<PrimaryKey> : ActorBase<PrimaryKey>, IFlowActor
    {
        private static readonly ConcurrentDictionary<Type, Func<object, IEvent, EventMeta, Task>> grainHandlerDict = new ConcurrentDictionary<Type, Func<object, IEvent, EventMeta, Task>>();
        private static readonly ConcurrentDictionary<Type, EventDiscardAttribute> eventDiscardAttributeDict = new ConcurrentDictionary<Type, EventDiscardAttribute>();
        private static readonly ConcurrentDictionary<Type, StrictHandleAttribute> eventStrictAttributerAttributeDict = new ConcurrentDictionary<Type, StrictHandleAttribute>();
        private readonly Func<object, IEvent, EventMeta, Task> handlerInvokeFunc;
        private readonly EventDiscardAttribute discardAttribute;

        public FlowActor()
        {
            this.ConcurrentHandle = this.ActorType.GetCustomAttributes(typeof(EventReentrantAttribute), true).Length > 0;
            this.discardAttribute = eventDiscardAttributeDict.GetOrAdd(this.ActorType, type =>
            {
                var handlerAttributes = this.ActorType.GetCustomAttributes(typeof(EventDiscardAttribute), false);
                if (handlerAttributes.Length > 0)
                {
                    return (EventDiscardAttribute)handlerAttributes[0];
                }
                else
                {
                    return default;
                }
            });
            var strictHandleAttributer = eventStrictAttributerAttributeDict.GetOrAdd(this.ActorType, type =>
            {
                var handlerAttributes = this.ActorType.GetCustomAttributes(typeof(StrictHandleAttribute), false);
                if (handlerAttributes.Length > 0)
                {
                    return (StrictHandleAttribute)handlerAttributes[0];
                }
                else
                {
                    return default;
                }
            });
            this.StrictHandle = strictHandleAttributer != default;
            this.handlerInvokeFunc = grainHandlerDict.GetOrAdd(this.ActorType, type =>
            {
                var methods = this.GetType().GetMethods().Where(m =>
                {
                    var parameters = m.GetParameters();
                    return parameters.Length >= 1 && parameters.Any(p => typeof(IEvent).IsAssignableFrom(p.ParameterType) && !p.ParameterType.IsInterface);
                }).ToList();
                var dynamicMethod = new DynamicMethod($"Handler_Invoke", typeof(Task), new Type[] { typeof(object), typeof(IEvent), typeof(EventMeta) }, type, true);
                var ilGen = dynamicMethod.GetILGenerator();
                var switchMethods = new List<SwitchMethodEmit>();
                for (int i = 0; i < methods.Count; i++)
                {
                    var method = methods[i];
                    var methodParams = method.GetParameters();
                    var caseType = methodParams.Single(p => typeof(IEvent).IsAssignableFrom(p.ParameterType)).ParameterType;
                    switchMethods.Add(new SwitchMethodEmit
                    {
                        Method = method,
                        CaseType = caseType,
                        DeclareLocal = ilGen.DeclareLocal(caseType),
                        Label = ilGen.DefineLabel(),
                        Parameters = methodParams,
                        Index = i
                    });
                }

                var sortList = new List<SwitchMethodEmit>();
                foreach (var item in switchMethods.Where(m => !typeof(IEvent).IsAssignableFrom(m.CaseType.BaseType)))
                {
                    sortList.Add(item);
                    GetInheritor(item, switchMethods, sortList);
                }

                sortList.Reverse();
                foreach (var item in switchMethods)
                {
                    if (!sortList.Contains(item))
                    {
                        sortList.Add(item);
                    }
                }

                var defaultLabel = ilGen.DefineLabel();
                var lastLable = ilGen.DefineLabel();
                var declare_1 = ilGen.DeclareLocal(typeof(Task));
                foreach (var item in sortList)
                {
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(OpCodes.Isinst, item.CaseType);
                    if (item.Index > 3)
                    {
                        if (item.DeclareLocal.LocalIndex > 0 && item.DeclareLocal.LocalIndex <= 255)
                        {
                            ilGen.Emit(OpCodes.Stloc_S, item.DeclareLocal);
                            ilGen.Emit(OpCodes.Ldloc_S, item.DeclareLocal);
                        }
                        else
                        {
                            ilGen.Emit(OpCodes.Stloc, item.DeclareLocal);
                            ilGen.Emit(OpCodes.Ldloc, item.DeclareLocal);
                        }
                    }
                    else
                    {
                        if (item.Index == 0)
                        {
                            ilGen.Emit(OpCodes.Stloc_0);
                            ilGen.Emit(OpCodes.Ldloc_0);
                        }
                        else if (item.Index == 1)
                        {
                            ilGen.Emit(OpCodes.Stloc_1);
                            ilGen.Emit(OpCodes.Ldloc_1);
                        }
                        else if (item.Index == 2)
                        {
                            ilGen.Emit(OpCodes.Stloc_2);
                            ilGen.Emit(OpCodes.Ldloc_2);
                        }
                        else
                        {
                            ilGen.Emit(OpCodes.Stloc_3);
                            ilGen.Emit(OpCodes.Ldloc_3);
                        }
                    }

                    ilGen.Emit(OpCodes.Brtrue, item.Label);
                }

                ilGen.Emit(OpCodes.Br, defaultLabel);
                foreach (var item in sortList)
                {
                    ilGen.MarkLabel(item.Label);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    // Load the first parameter
                    if (item.Parameters[0].ParameterType == item.CaseType)
                    {
                        LdEventArgs(item, ilGen);
                    }
                    else if (item.Parameters[0].ParameterType == typeof(EventMeta))
                    {
                        ilGen.Emit(OpCodes.Ldarg_2);
                    }

                    // Load the second parameter
                    if (item.Parameters.Length >= 2)
                    {
                        if (item.Parameters[1].ParameterType == item.CaseType)
                        {
                            LdEventArgs(item, ilGen);
                        }
                        else if (item.Parameters[1].ParameterType == typeof(EventMeta))
                        {
                            ilGen.Emit(OpCodes.Ldarg_2);
                        }
                    }

                    // Load the third parameter
                    if (item.Parameters.Length >= 3)
                    {
                        if (item.Parameters[2].ParameterType == item.CaseType)
                        {
                            LdEventArgs(item, ilGen);
                        }
                        else if (item.Parameters[2].ParameterType == typeof(EventMeta))
                        {
                            ilGen.Emit(OpCodes.Ldarg_2);
                        }

                    }

                    ilGen.Emit(OpCodes.Call, item.Method);
                    if (item.DeclareLocal.LocalIndex > 0 && item.DeclareLocal.LocalIndex <= 255)
                    {
                        ilGen.Emit(OpCodes.Stloc_S, declare_1);
                    }
                    else
                    {
                        ilGen.Emit(OpCodes.Stloc, declare_1);
                    }

                    ilGen.Emit(OpCodes.Br, lastLable);
                }

                ilGen.MarkLabel(defaultLabel);
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldarg_1);
                ilGen.Emit(OpCodes.Call, type.GetMethod(nameof(this.DefaultHandler)));
                if (declare_1.LocalIndex > 0 && declare_1.LocalIndex <= 255)
                {
                    ilGen.Emit(OpCodes.Stloc_S, declare_1);
                }
                else
                {
                    ilGen.Emit(OpCodes.Stloc, declare_1);
                }

                ilGen.Emit(OpCodes.Br, lastLable);
                //last
                ilGen.MarkLabel(lastLable);
                if (declare_1.LocalIndex > 0 && declare_1.LocalIndex <= 255)
                {
                    ilGen.Emit(OpCodes.Ldloc_S, declare_1);
                }
                else
                {
                    ilGen.Emit(OpCodes.Ldloc, declare_1);
                }

                ilGen.Emit(OpCodes.Ret);
                var parames = new ParameterExpression[] { Expression.Parameter(typeof(object)), Expression.Parameter(typeof(IEvent)), Expression.Parameter(typeof(EventMeta)) };
                var body = Expression.Call(dynamicMethod, parames);
                return Expression.Lambda<Func<object, IEvent, EventMeta, Task>>(body, parames).Compile();
            });
            // Load Event parameters
            static void LdEventArgs(SwitchMethodEmit item, ILGenerator gen)
            {
                if (item.Index > 3)
                {
                    if (item.DeclareLocal.LocalIndex > 0 && item.DeclareLocal.LocalIndex <= 255)
                    {
                        gen.Emit(OpCodes.Ldloc_S, item.DeclareLocal);
                    }
                    else
                    {
                        gen.Emit(OpCodes.Ldloc, item.DeclareLocal);
                    }
                }
                else
                {
                    if (item.Index == 0)
                    {
                        gen.Emit(OpCodes.Ldloc_0);
                    }
                    else if (item.Index == 1)
                    {
                        gen.Emit(OpCodes.Ldloc_1);
                    }
                    else if (item.Index == 2)
                    {
                        gen.Emit(OpCodes.Ldloc_2);
                    }
                    else
                    {
                        gen.Emit(OpCodes.Ldloc_3);
                    }
                }
            }

            static void GetInheritor(SwitchMethodEmit from, List<SwitchMethodEmit> list, List<SwitchMethodEmit> result)
            {
                var inheritorList = list.Where(m => m.CaseType.BaseType == from.CaseType);
                foreach (var inheritor in inheritorList)
                {
                    result.Add(inheritor);
                    GetInheritor(inheritor, list, result);
                }
            }
        }
        #region property
        protected SubActorOptions VertexOptions { get; private set; }
        public abstract IVertexActor Vertex { get; }
        protected ILogger Logger { get; private set; }

        protected ISerializer Serializer { get; private set; }

        protected IEventTypeContainer EventTypeContainer { get; private set; }
        /// <summary>
        /// Memory state, restored by snapshot + Event play or replay
        /// </summary>
        protected SubSnapshot<PrimaryKey> Snapshot { get; set; }
        /// <summary>
        /// The event version number of the snapshot
        /// </summary>
        protected long ActivateSnapshotVersion { get; private set; }
        public ISubSnapshotStorage<PrimaryKey> SnapshotStorage { get; private set; }
        /// <summary>
        /// Whether to enable concurrent event processing
        /// </summary>
        protected bool ConcurrentHandle { get; set; }
        /// <summary>
        /// Is the incident strictly checked for handle
        /// </summary>
        protected bool StrictHandle { get; set; }
        /// <summary>
        /// List of unprocessed events
        /// </summary>
        private List<EventUnit<PrimaryKey>> UnprocessedEventList { get; set; } = new List<EventUnit<PrimaryKey>>();
        #endregion
        #region Activate
        /// <summary>
        /// Unified method of dependency injection
        /// </summary>
        /// <returns></returns>
        protected virtual async ValueTask DependencyInjection()
        {
            this.VertexOptions = this.ServiceProvider.GetService<IOptionsSnapshot<SubActorOptions>>().Get(this.ActorType.FullName);
            this.Serializer = this.ServiceProvider.GetService<ISerializer>();
            this.EventTypeContainer = this.ServiceProvider.GetService<IEventTypeContainer>();
            this.Logger = (ILogger)this.ServiceProvider.GetService(typeof(ILogger<>).MakeGenericType(this.ActorType));

            var snapshotStorageFactory = this.ServiceProvider.GetService<ISubSnapshotStorageFactory>();
            this.SnapshotStorage = await snapshotStorageFactory.Create(this);
        }
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            await DependencyInjection();

            if (this.ConcurrentHandle)
            {
                this.UnprocessedEventList = new List<EventUnit<PrimaryKey>>();
            }

            try
            {
                await this.ReadSnapshotAsync();
                if (this.Snapshot.Version != 0 || this.VertexOptions.InitType == SubInitType.ZeroVersion)
                    await this.Recovery();

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
        protected virtual async Task ReadSnapshotAsync()
        {
            try
            {
                this.Snapshot = await this.SnapshotStorage.Get(this.ActorId);
                if (this.Snapshot == null)
                {
                    await this.CreateSnapshot();
                }

                this.ActivateSnapshotVersion = this.Snapshot.Version;
                if (this.Logger.IsEnabled(LogLevel.Trace))
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
            this.Snapshot = new SubSnapshot<PrimaryKey>
            {
                ActorId = this.ActorId
            };
            return ValueTask.CompletedTask;
        }
        /// <summary>
        /// Restore from library
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        private async Task Recovery()
        {
            while (true)
            {
                var documentList = await this.Vertex.GetEventDocuments(this.Snapshot.Version + 1, this.Snapshot.Version + this.VertexOptions.EventPageSize);
                var evtList = ConvertToEventUnitList(documentList);
                await this.UnsafeTell(evtList);
                if (documentList.Count < this.VertexOptions.EventPageSize)
                {
                    break;
                }
            }
        }
        private List<EventUnit<PrimaryKey>> ConvertToEventUnitList(IList<EventDocumentDto> documents)
        {
            return documents.Select(document =>
             {
                 if (!EventTypeContainer.TryGet(document.Name, out var type))
                 {
                     throw new NoNullAllowedException($"event name of {document.Name}");
                 }
                 var data = Serializer.Deserialize(document.Data, type);
                 return new EventUnit<PrimaryKey>
                 {
                     ActorId = this.ActorId,
                     Event = data as IEvent,
                     Meta = new EventMeta { Version = document.Version, Timestamp = document.Timestamp, FlowId = document.FlowId }
                 };
             }).ToList();
        }
        #endregion
        public Task OnNext(Immutable<byte[]> bytes)
        {
            return this.OnNext(bytes.Value);
        }

        public async Task OnNext(Immutable<List<byte[]>> items)
        {
            if (this.ConcurrentHandle)
            {
                var startVersion = this.Snapshot.Version;
                if (this.UnprocessedEventList.Count > 0)
                {
                    startVersion = this.UnprocessedEventList.Last().Meta.Version;
                }

                var evtList = items.Value.Select(bytes =>
                {
                    if (TryConvertToEventUnit(bytes, out var data))
                    {
                        return data;
                    }
                    else
                    {
                        this.Logger.LogError(new ArgumentException(nameof(bytes)), "Deserialization failed");
                    }
                    return default;
                }).Where(o => o != null && o.Meta.Version > startVersion).OrderBy(o => o.Meta.Version);

                await this.ConcurrentTell(evtList);

                if (this.Logger.IsEnabled(LogLevel.Trace))
                {
                    this.Logger.LogTrace("OnNext concurrent completed: {0}->{1}->{2}", this.ActorType.FullName, this.ActorId.ToString(), this.Serializer.Serialize(evtList));
                }
            }
            else
            {
                foreach (var bytes in items.Value)
                {
                    await this.OnNext(bytes);
                }
            }
        }

        private async Task OnNext(byte[] bytes)
        {
            if (TryConvertToEventUnit(bytes, out var data))
            {
                await this.Tell(data);
                if (this.Logger.IsEnabled(LogLevel.Trace))
                {
                    this.Logger.LogTrace("OnNext completed: {0}->{1}->{2}", this.ActorType.FullName, this.ActorId.ToString(), this.Serializer.Serialize(data));
                }
            }
            else
            {
                this.Logger.LogError(new ArgumentException(nameof(bytes)), "Deserialization failed");
            }
        }
        protected bool TryConvertToEventUnit(byte[] bytes, out EventUnit<PrimaryKey> eventUnit)
        {
            if (EventConverter.TryParseWithNoId(bytes, out var transport) &&
                   this.EventTypeContainer.TryGet(transport.EventName, out var type))
            {
                var data = this.Serializer.Deserialize(transport.EventBytes, type);
                if (data is IEvent @event)
                {
                    var eventMeta = transport.MetaBytes.ParseToEventMeta();
                    eventUnit = new EventUnit<PrimaryKey>
                    {
                        ActorId = this.ActorId,
                        Meta = eventMeta,
                        Event = @event
                    };
                    return true;
                }
            }
            eventUnit = default;
            return false;
        }
        public Task DefaultHandler(IEvent evt)
        {
            if (StrictHandle && (this.discardAttribute is null || !this.discardAttribute.Discards.Contains(evt.GetType())))
            {
                throw new MissingMethodException(evt.GetType().FullName);
            }
            return Task.CompletedTask;
        }
        protected async ValueTask Tell(EventUnit<PrimaryKey> eventUnit)
        {
            try
            {
                if (eventUnit.Meta.Version <= this.Snapshot.Version)
                    return;

                if (eventUnit.Meta.Version == this.Snapshot.Version + 1)
                {
                    await this.EventDelivered(eventUnit);

                    this.Snapshot.FullUpdateVersion(eventUnit.Meta, this.ActorType);//Version of the update process
                }
                else if (eventUnit.Meta.Version > this.Snapshot.Version)
                {
                    var documents = await this.Vertex.GetEventDocuments(this.Snapshot.Version + 1, eventUnit.Meta.Version - 1);
                    var eventList = ConvertToEventUnitList(documents);
                    foreach (var evt in eventList)
                    {
                        await this.EventDelivered(evt);

                        this.Snapshot.FullUpdateVersion(evt.Meta, this.ActorType);//Version of the update process
                    }
                }

                if (eventUnit.Meta.Version == this.Snapshot.Version + 1)
                {
                    await this.EventDelivered(eventUnit);
                    this.Snapshot.FullUpdateVersion(eventUnit.Meta, this.ActorType);//Version of the update process
                }

                if (eventUnit.Meta.Version > this.Snapshot.Version)
                {
                    throw new EventVersionException(this.ActorId.ToString(), this.ActorType, eventUnit.Meta.Version, this.Snapshot.Version);
                }

                await this.SaveSnapshotAsync();
            }
            catch
            {
                await this.SaveSnapshotAsync(true);
                throw;
            }
        }
        protected async Task ConcurrentTell(IEnumerable<EventUnit<PrimaryKey>> inputs)
        {
            var startVersion = this.Snapshot.Version;
            if (this.UnprocessedEventList.Count > 0)
            {
                startVersion = this.UnprocessedEventList.Last().Meta.Version;
            }
            var evtList = inputs.Where(e => e.Meta.Version > startVersion).ToList();
            if (evtList.Count > 0)
            {
                var inputLast = evtList.Last();
                if (startVersion + evtList.Count < inputLast.Meta.Version)
                {
                    var documents = await this.Vertex.GetEventDocuments(startVersion + 1, inputLast.Meta.Version - 1);
                    var loadList = ConvertToEventUnitList(documents);
                    this.UnprocessedEventList.AddRange(loadList);
                    this.UnprocessedEventList.Add(inputLast);
                }
                else
                {
                    this.UnprocessedEventList.AddRange(evtList);
                }
            }

            if (this.UnprocessedEventList.Count > 0)
            {
                await Task.WhenAll(this.UnprocessedEventList.Select(@event => this.EventDelivered(@event).AsTask()));
                this.Snapshot.UnsafeUpdateVersion(this.UnprocessedEventList.Last().Meta);
                await this.SaveSnapshotAsync();

                this.UnprocessedEventList.Clear();
            }
        }
        protected virtual async Task UnsafeTell(IEnumerable<EventUnit<PrimaryKey>> eventList)
        {
            if (this.ConcurrentHandle)
            {
                await Task.WhenAll(eventList.Select(@event =>
                {
                    return this.EventDelivered(@event).AsTask();
                }));
                var lastEvt = eventList.Last();
                this.Snapshot.UnsafeUpdateVersion(lastEvt.Meta);
            }
            else
            {
                foreach (var @event in eventList)
                {
                    this.Snapshot.IncrementDoingVersion(this.ActorType);//Mark the Version to be processed
                    await this.EventDelivered(@event);

                    this.Snapshot.UpdateVersion(@event.Meta, this.ActorType);//Version of the update process
                }
            }

            await this.SaveSnapshotAsync();
        }
        protected virtual ValueTask EventDelivered(EventUnit<PrimaryKey> eventUnit)
        {
            try
            {
                RequestContext.Set(RuntimeConsts.EventFlowIdKey, eventUnit.Meta.FlowId);
                return this.OnEventDelivered(eventUnit);
            }
            catch (Exception ex)
            {
                this.Logger.LogCritical(ex, "Delivered failed: {0}->{1}->{2}", this.ActorType.FullName, this.ActorId.ToString(), this.Serializer.Serialize(eventUnit, eventUnit.GetType()));
                throw;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnEventDelivered(EventUnit<PrimaryKey> eventUnit)
        {
            return new ValueTask(this.handlerInvokeFunc(this, eventUnit.Event, eventUnit.Meta));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnSaveSnapshot() => ValueTask.CompletedTask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ValueTask OnSavedSnapshot() => ValueTask.CompletedTask;

        protected virtual async ValueTask SaveSnapshotAsync(bool force = false)
        {
            if ((force && this.Snapshot.Version > this.ActivateSnapshotVersion) ||
                    (this.Snapshot.Version - this.ActivateSnapshotVersion >= this.VertexOptions.SnapshotVersionInterval))
            {
                try
                {
                    await this.OnSaveSnapshot();//Custom save items

                    if (this.ActivateSnapshotVersion == 0)
                    {
                        await this.SnapshotStorage.Insert(this.Snapshot);
                    }
                    else
                    {
                        await this.SnapshotStorage.Update(this.Snapshot);
                    }

                    this.ActivateSnapshotVersion = this.Snapshot.Version;
                    await this.OnSavedSnapshot();

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

    }
}
