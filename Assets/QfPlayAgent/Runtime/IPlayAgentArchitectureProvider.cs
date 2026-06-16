using QFramework;

namespace QfPlayAgent
{
    public interface IPlayAgentArchitectureProvider
    {
        string ArchitectureId { get; }
        bool IsReady { get; }
        IArchitecture GetArchitecture();
    }
}
