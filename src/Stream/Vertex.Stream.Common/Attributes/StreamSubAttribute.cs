using System;

namespace Vertex.Stream.Common
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StreamSubAttribute : Attribute
    {
        public StreamSubAttribute(string name, string group, int sharding = 1)
        {
            this.Group = group;
            this.Name = name;
            if (sharding < 0)
                throw new ArgumentOutOfRangeException("sharding must be greater than 0");
            this.Sharding = sharding;
        }
        /// <summary>
        /// Listener group
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Listener name (if it is shadow, please set to null)
        /// </summary>
        public string Name { get; set; }
        public int Sharding { get; set; }
    }
}
