using QFramework;

namespace QfPlayAgent
{
    public interface IPlayAgentSnapshotProvider
    {
        object Capture(IArchitecture architecture);
    }
}
