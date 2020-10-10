using System;

namespace Vertex.Storage.Linq2db.Core
{
    public class EventArchivePolicy
    {
        public EventArchivePolicy(string name, Func<string, long, string> sharding, Func<string, bool> filter)
        {
            this.Name = name;
            this.Sharding = sharding;
            this.Filter = filter;
        }
        public string Name { get; set; }
        /// <summary>
        /// 分表策略
        /// </summary>
        public Func<string, long, string> Sharding { get; set; }
        /// <summary>
        /// 用于从当前数据库所有表中过滤出属于归档表的表
        /// </summary>
        public Func<string, bool> Filter { get; set; }
    }
}
