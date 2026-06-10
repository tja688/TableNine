using QFramework;
using UnityEngine;

public sealed class DebugPingCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        Debug.Log("DebugPingCommand executed.");
    }
}
