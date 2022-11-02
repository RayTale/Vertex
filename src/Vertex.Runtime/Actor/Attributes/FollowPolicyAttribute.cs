using System;

namespace Vertex.Runtime.Actor.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class FollowPolicyAttribute : Attribute
    {
        public FollowPolicyAttribute(string optionPolicy)
        {
            this.OptionPolicy = optionPolicy;
        }

        public string OptionPolicy { get; set; }
    }
}
