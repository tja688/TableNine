using System;

namespace QfPlayAgent
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class QfArgAttribute : Attribute
    {
        public QfArgAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; }
        public bool Required { get; set; } = true;
    }
}
