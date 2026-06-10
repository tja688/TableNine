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
        TableNine.InitArchitecture();
    }
}
