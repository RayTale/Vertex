using System;

namespace Vertex.Runtime.Actor.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class VertexPolicyAttribute : Attribute
    {
        public VertexPolicyAttribute(string optionPolicy, string archiveOptionPolicy)
        {
            this.OptionPolicy = optionPolicy;
            this.ArchiveOptionPolicy = archiveOptionPolicy;
        }

        public string OptionPolicy { get; set; }

        public string ArchiveOptionPolicy { get; set; }
    }
}
