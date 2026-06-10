using QFramework;
using UnityEngine;

public sealed class GameplayBootstrap : MonoBehaviour
{
    private bool mBootstrapped;

    private void Awake()
    {
        Bootstrap();
    }

    private void Bootstrap()
    {
        if (mBootstrapped)
        {
            return;
        }

        mBootstrapped = true;

        ResKit.Init();
        _ = UIKit.Root;
        TableNine.InitArchitecture();

        Debug.Log("TableNine architecture initialized");

        if (Application.isEditor || Debug.isDebugBuild)
        {
            TableNine.Interface.SendCommand(new DebugPingCommand());
        }
    }
}
