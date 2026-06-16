using System;
using System.Collections.Generic;

namespace QfPlayAgent
{
    public sealed class PlayAgentArgDescriptor
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }
        public string DefaultValue { get; set; }
    }

    public sealed class PlayAgentCommandDescriptor
    {
        public string Name { get; set; }
        public string FullTypeName { get; set; }
        public string Description { get; set; }
        public QfAiRisk Risk { get; set; }
        public int DefaultWaitFrames { get; set; }
        public List<PlayAgentArgDescriptor> Args { get; } = new List<PlayAgentArgDescriptor>();
    }
}
