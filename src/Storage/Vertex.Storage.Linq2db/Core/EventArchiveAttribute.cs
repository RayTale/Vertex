using System;

namespace Vertex.Storage.Linq2db.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EventArchiveAttribute : Attribute
    {
        public EventArchiveAttribute(string optionName, string name, string policy)
        {
            this.Name = name;
            this.OptionName = optionName;
            this.Policy = policy;
        }

        public string Policy { get; set; }

        public string OptionName { get; set; }

        public string Name { get; set; }
    }
}
