using System;
using System.Linq;
using Vertex.Utils;

namespace Vertex.Storage.Linq2db.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TxEventStorageAttribute : TxEventStorageBaseAttribute
    {
        private readonly Func<string, string> shardingFunc;
        private readonly string optionName;

        public TxEventStorageAttribute(string optionName, string name, int sharding = 0)
        {
            this.Name = name;
            this.optionName = optionName;
            if (sharding < 0)
            {
                throw new ArgumentOutOfRangeException("sharding must be greater than 0");
            }

            if (sharding == 0)
            {
                this.shardingFunc = actorId => $"Vertex_TxEvent_{name}".ToLower();
            }
            else
            {
                var tableNames = Enumerable.Range(0, sharding).Select(index => $"Vertex_TxEvent_{name}_{index}".ToLower()).ToList();
                var hash = new ConsistentHash(tableNames, tableNames.Count * 10);
                this.shardingFunc = actorId => hash.GetNode(actorId);
            }
        }

        public string Name { get; init; }

        public override string GetOptionName(string actorId)
        {
            return this.optionName;
        }

        public override string GetTableName(string actorId)
        {
            return this.shardingFunc(actorId);
        }
    }
}
