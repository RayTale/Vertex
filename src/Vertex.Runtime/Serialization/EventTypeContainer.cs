using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using Vertex.Abstractions.Event;
using Vertex.Utils;
using Vertext.Abstractions.Event;

namespace Vertex.Runtime.Serialization
{
    public class EventTypeContainer : IEventTypeContainer
    {
        private readonly ConcurrentDictionary<string, Type> nameDict = new();
        private readonly ConcurrentDictionary<Type, string> typeDict = new();
        private readonly ILogger<EventTypeContainer> logger;

        public EventTypeContainer(ILogger<EventTypeContainer> logger, IEventNameGenerator eventNameGenerator)
        {
            this.logger = logger;
            var baseEventType = typeof(IEvent);
            var attributeType = typeof(EventNameAttribute);
            foreach (var assembly in AssemblyHelper.GetAssemblies(this.logger))
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (baseEventType.IsAssignableFrom(type))
                    {
                        string eventName;
                        var attribute = type.GetCustomAttributes(attributeType, false).FirstOrDefault();
                        if (attribute != null && attribute is EventNameAttribute nameAttribute
                                              && nameAttribute.Name != default)
                        {
                            eventName = nameAttribute.Name;
                        }
                        else
                        {
                            eventName = eventNameGenerator.GetName(type);
                        }
                        if (!this.nameDict.TryAdd(eventName, type))
                        {
                            throw new OverflowException(eventName);
                        }

                        this.typeDict.TryAdd(type, eventName);
                    }
                }
            }
        }

        public bool TryGet(string name, out Type type)
        {
            var value = this.nameDict.GetOrAdd(name, key =>
            {
                foreach (var assembly in AssemblyHelper.GetAssemblies(this.logger))
                {
                    var type = assembly.GetType(name, false);
                    if (type != default)
                    {
                        return type;
                    }
                }

                return Type.GetType(name, false);
            });
            type = value;

            return value != default;
        }

        public bool TryGet(Type type, out string name)
        {
            return this.typeDict.TryGetValue(type, out name);
        }
    }
}
