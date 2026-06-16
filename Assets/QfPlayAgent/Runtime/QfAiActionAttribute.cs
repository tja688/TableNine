using System;

namespace QfPlayAgent
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class QfAiActionAttribute : Attribute
    {
        public QfAiActionAttribute(string name = null)
        {
            Name = name;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public QfAiRisk Risk { get; set; } = QfAiRisk.PlayerInput;
        public bool ExposeToAgent { get; set; } = true;
        public int DefaultWaitFrames { get; set; } = 1;
    }
}
