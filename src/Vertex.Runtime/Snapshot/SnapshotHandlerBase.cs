﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using Vertex.Abstractions.Attributes;
using Vertex.Abstractions.Snapshot;
using Vertex.Utils.Emit;
using Vertext.Abstractions.Event;

namespace Vertex.Runtime.Snapshot
{
    public abstract class SnapshotHandlerBase<TPrimaryKey, T> : ISnapshotHandler<TPrimaryKey, T>
          where T : ISnapshot
    {
        private readonly Action<object, T, IEvent, EventMeta> handlerInvokeFunc;
        private readonly EventDiscardAttribute eventDiscardAttribute;
        private readonly StrictHandleAttribute eventStrictAttributer;

        public SnapshotHandlerBase()
        {
            var thisType = this.GetType();
            var handlerDiscardAttributes = thisType.GetCustomAttributes(typeof(EventDiscardAttribute), false);
            if (handlerDiscardAttributes.Length > 0)
            {
                this.eventDiscardAttribute = (EventDiscardAttribute)handlerDiscardAttributes[0];
            }
            else
            {
                this.eventDiscardAttribute = default;
            }
            var handlerStrictAttributes = thisType.GetCustomAttributes(typeof(StrictHandleAttribute), false);
            if (handlerStrictAttributes.Length > 0)
            {
                this.eventStrictAttributer = (StrictHandleAttribute)handlerStrictAttributes[0];
            }
            else
            {
                this.eventStrictAttributer = default;
            }
            var methods = this.GetType().GetMethods().Where(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length >= 2 && parameters.Any(p => p.ParameterType == typeof(T)) && parameters.Any(p => typeof(IEvent).IsAssignableFrom(p.ParameterType) && !p.ParameterType.IsInterface);
            }).ToList();
            var dynamicMethod = new DynamicMethod($"Handler_Invoke", typeof(void), new Type[] { typeof(object), typeof(T), typeof(IEvent), typeof(EventMeta) }, thisType, true);
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
            foreach (var item in sortList)
            {
                ilGen.Emit(OpCodes.Ldarg_2);
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

                // 加载第一个参数
                if (item.Parameters[0].ParameterType == typeof(T))
                {
                    ilGen.Emit(OpCodes.Ldarg_1);
                }
                else if (item.Parameters[0].ParameterType == typeof(EventMeta))
                {
                    ilGen.Emit(OpCodes.Ldarg_3);
                }
                else
                {
                    LdEventArgs(item, ilGen);
                }

                // 加载第二个参数
                if (item.Parameters[1].ParameterType == typeof(T))
                {
                    ilGen.Emit(OpCodes.Ldarg_1);
                }
                else if (item.Parameters[1].ParameterType == typeof(EventMeta))
                {
                    ilGen.Emit(OpCodes.Ldarg_3);
                }
                else
                {
                    LdEventArgs(item, ilGen);
                }

                // 加载第三个参数
                if (item.Parameters.Length == 3)
                {
                    if (item.Parameters[1].ParameterType == typeof(T))
                    {
                        ilGen.Emit(OpCodes.Ldarg_1);
                    }
                    else if (item.Parameters[1].ParameterType == typeof(EventMeta))
                    {
                        ilGen.Emit(OpCodes.Ldarg_3);
                    }
                    else
                    {
                        LdEventArgs(item, ilGen);
                    }
                }

                ilGen.Emit(OpCodes.Call, item.Method);
                ilGen.Emit(OpCodes.Ret);
            }

            ilGen.MarkLabel(defaultLabel);
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_2);
            ilGen.Emit(OpCodes.Call, thisType.GetMethod(nameof(this.DefaultHandler)));
            ilGen.Emit(OpCodes.Ret);
            var parames = new ParameterExpression[] { Expression.Parameter(typeof(object)), Expression.Parameter(typeof(T)), Expression.Parameter(typeof(IEvent)), Expression.Parameter(typeof(EventMeta)) };
            var body = Expression.Call(dynamicMethod, parames);
            this.handlerInvokeFunc = Expression.Lambda<Action<object, T, IEvent, EventMeta>>(body, parames).Compile();

            // 加载Event参数
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

        public virtual void Apply(SnapshotUnit<TPrimaryKey, T> snapshotBox, EventUnit<TPrimaryKey> eventBox)
        {
            this.handlerInvokeFunc(this, snapshotBox.Data, eventBox.Event, eventBox.Meta);
        }

        public virtual void DefaultHandler(IEvent evt)
        {
            if (this.eventStrictAttributer != default
                && (this.eventDiscardAttribute is null || !this.eventDiscardAttribute.Discards.Contains(evt.GetType())))
            {
                throw new NotSupportedException(evt.GetType().ToString());
            }
        }
    }
}
