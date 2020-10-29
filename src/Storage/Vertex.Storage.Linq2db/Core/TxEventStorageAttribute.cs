using System;
using System.Linq;
using Vertex.Utils;

namespace Vertex.Storage.Linq2db.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TxEventStorageAttribute : Attribute
    {
        public TxEventStorageAttribute(string optionName, string name, int sharding = 0)
        {
            this.Name = name;
            this.OptionName = optionName;
            if (sharding < 0)
            {
                throw new ArgumentOutOfRangeException("sharding must be greater than 0");
            }

            if (sharding == 0)
            {
                this.ShardingFunc = actorId => $"Vertex_TxEvent_{name}".ToLower();
            }
            else
            {
                var tableNames = Enumerable.Range(0, sharding).Select(index => $"Vertex_TxEvent_{name}_{index}".ToLower()).ToList();
                var hash = new ConsistentHash(tableNames, tableNames.Count * 10);
                this.ShardingFunc = actorId => hash.GetNode(actorId);
            }
        }

        public string OptionName { get; init; }

        public string Name { get; init; }

        public Func<string, string> ShardingFunc { get; init; }
    }
}
