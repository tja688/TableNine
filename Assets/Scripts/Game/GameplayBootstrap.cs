using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

public sealed class GameplayBootstrap : MonoBehaviour
{
    [SerializeField] private TableNineUIPanelRegistry mUIPanelRegistry;
    [SerializeField] private bool mOpenLegacyHudOnBoot;
    [SerializeField] private bool mAutoStartLegacyRun;

    private bool mBootstrapped;
    private TableNineUIRouter mUIRouter;

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
        TableNine.ConfigureRuntimePersistence();
        TableNine.InitArchitecture();
        SetupUIKit();

        if (mAutoStartLegacyRun &&
            !this.GetArchitecture().GetModel<IRunModel>().IsRunActive.Value)
        {
            this.GetArchitecture().SendCommand(new StartNewRunCommand());
        }
    }

    private void OnDestroy()
    {
        mUIRouter?.Dispose();
        mUIRouter = null;
    }

    private void SetupUIKit()
    {
        if (mUIPanelRegistry == null)
        {
            Debug.LogError("GameplayBootstrap requires a TableNineUIPanelRegistry.");
            return;
        }

        UIKit.Config = new TableNineUIKitConfig(mUIPanelRegistry);
        DescriptionPanelTexts.Initialize(mUIPanelRegistry != null ? mUIPanelRegistry.DescriptionPanelConfig : null);
        DisableSceneEventSystemBeforeUIKitRoot();
        UIKit.Root.SetResolution(426, 240, 0.5f);
        UIKit.Root.ScreenSpaceOverlayRenderMode();
        UIKit.CloseAllPanel();
        if (mOpenLegacyHudOnBoot)
        {
            UIKit.OpenPanel<UIGameplayPanel>(UILevel.Common);
        }

        mUIRouter?.Dispose();
        mUIRouter = new TableNineUIRouter(mUIPanelRegistry);
        mUIRouter.Start();
        EnsureDebugPanel();
    }

    private static void EnsureDebugPanel()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Object.FindObjectOfType<UIDebugPanel>() != null)
        {
            return;
        }

        var canvasObject = new GameObject("UIDebugPanelHost");
        Object.DontDestroyOnLoad(canvasObject);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var root = new GameObject("Root");
        root.transform.SetParent(canvasObject.transform, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var textObject = new GameObject("Info");
        textObject.transform.SetParent(root.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.02f, 0.15f);
        textRect.anchorMax = new Vector2(0.98f, 0.98f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObject.AddComponent<UnityEngine.UI.Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;
        text.supportRichText = false;

        var buttonBar = new GameObject("Buttons");
        buttonBar.transform.SetParent(root.transform, false);
        var buttonBarRect = buttonBar.AddComponent<RectTransform>();
        buttonBarRect.anchorMin = new Vector2(0.02f, 0.02f);
        buttonBarRect.anchorMax = new Vector2(0.98f, 0.12f);
        buttonBarRect.offsetMin = Vector2.zero;
        buttonBarRect.offsetMax = Vector2.zero;
        var buttonLayout = buttonBar.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        buttonLayout.spacing = 4f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = true;

        var panel = canvasObject.AddComponent<UIDebugPanel>();
        CreateDebugButton(buttonBar.transform, "保存", panel.OnClickSave);
        CreateDebugButton(buttonBar.transform, "读档", panel.OnClickLoad);
        CreateDebugButton(buttonBar.transform, "重放", panel.OnClickReplay);
        CreateDebugButton(buttonBar.transform, "Bug", panel.OnClickCopyBugReport);

        var panelField = typeof(UIDebugPanel).GetField("mRoot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var textField = typeof(UIDebugPanel).GetField("mInfoText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        panelField?.SetValue(panel, root);
        textField?.SetValue(panel, text);
#endif
    }

    private static void CreateDebugButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(label);
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
        var button = buttonObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonObject.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObject.AddComponent<UnityEngine.UI.Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 11;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
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
