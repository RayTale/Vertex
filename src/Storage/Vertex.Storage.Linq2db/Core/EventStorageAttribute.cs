using System;
using System.Linq;
using Vertex.Utils;

namespace Vertex.Storage.Linq2db.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EventStorageAttribute : Attribute
    {
        public EventStorageAttribute(string optionName, string name, int sharding = 0)
        {
            Name = name;
            OptionName = optionName;
            if (sharding < 0)
                throw new ArgumentOutOfRangeException("sharding must be greater than 0");
            if (sharding == 0)
                ShardingFunc = actorId => $"Vertex_Event_{name}".ToLower();
            else
            {
                var tableNames = Enumerable.Range(0, sharding).Select(index => $"Vertex_Event_{name}_{index}".ToLower()).ToList();
                var hash = new ConsistentHash(tableNames, tableNames.Count * 10);
                ShardingFunc = actorId => hash.GetNode(actorId);
            }
        }
        public string OptionName { get; init; }
        public string Name { get; init; }
        public Func<string, string> ShardingFunc { get; init; }
    }
}
