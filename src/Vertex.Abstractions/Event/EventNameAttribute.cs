using System;

namespace Vertex.Abstractions.Event
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EventNameAttribute : Attribute
    {
        public EventNameAttribute(string name) => this.Name = name;

        /// <summary>
        /// 类型唯一码
        /// </summary>
        public string Name { get; set; }
    }
}
