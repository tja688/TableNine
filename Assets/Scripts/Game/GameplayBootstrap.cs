using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class GameplayBootstrap : MonoBehaviour
{
    [SerializeField] private TableNineUIPanelRegistry mUIPanelRegistry;

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
        SetupUIKit();

        if (!this.GetArchitecture().GetModel<IRunModel>().IsRunActive.Value)
        {
            this.GetArchitecture().SendCommand(new StartNewRunCommand());
        }
    }

    private void SetupUIKit()
    {
        if (mUIPanelRegistry == null)
        {
            Debug.LogError("GameplayBootstrap requires a TableNineUIPanelRegistry.");
            return;
        }

        UIKit.Config = new TableNineUIKitConfig(mUIPanelRegistry);
        DisableSceneEventSystemBeforeUIKitRoot();
        UIKit.Root.SetResolution(426, 240, 0.5f);
        UIKit.Root.ScreenSpaceOverlayRenderMode();
        UIKit.CloseAllPanel();
        UIKit.OpenPanel<UIGameplayPanel>(UILevel.Common);
    }

    private static void DisableSceneEventSystemBeforeUIKitRoot()
    {
        var eventSystems = FindObjectsOfType<EventSystem>();
        for (var i = 0; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] != null && eventSystems[i].transform.root.name != "UIRoot")
            {
                eventSystems[i].gameObject.SetActive(false);
            }
        }
    }

    private IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }
}
