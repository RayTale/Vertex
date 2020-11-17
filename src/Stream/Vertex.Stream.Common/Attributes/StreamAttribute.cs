using System;
using System.Linq;
using Vertex.Utils;

namespace Vertex.Stream.Common
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StreamAttribute : Attribute
    {
        public StreamAttribute(string name, int sharding = 1)
        {
            this.Name = name;
            if (sharding < 0)
            {
                throw new ArgumentOutOfRangeException("sharding must be greater than 0");
            }

            if (sharding == 0)
            {
                this.ShardingFunc = actorId => name;
            }
            else
            {
                var tableNames = Enumerable.Range(0, sharding).Select(index => index == 0 ? name : $"{name}_{index}").ToList();
                var hash = new ConsistentHash(tableNames, tableNames.Count * 10);
                this.ShardingFunc = actorId => hash.GetNode(actorId);
            }
        }

        /// <summary>
        /// Listener name (if it is shadow, please set to null)
        /// </summary>
        public string Name { get; set; }

        public Func<string, string> ShardingFunc { get; init; }
    }
}
