using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertex.Abstractions.Attributes
{
    /// <summary>
    /// EventHandler配置信息
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EventDiscardAttribute : Attribute
    {
        public EventDiscardAttribute(params Type[] discards)
        {
            this.Discards = discards.ToList();
        }

        /// <summary>
        /// 需要忽略的Event类型，不然系统会强制检查并抛出异常
        /// </summary>
        public List<Type> Discards { get; set; }
    }
}
