using UnityEngine;

/// <summary>
/// 场景中的碎裂调参入口。挂到任意常驻物体（如 Gameplay 控制器）即可在 Inspector 调参。
/// </summary>
[DisallowMultipleComponent]
public sealed class CardFakeShatterTuningProfile : MonoBehaviour
{
    [Tooltip("可选：指定 ScriptableObject 则完全使用资产参数；留空则使用下方 Inline Tuning。")]
    [SerializeField] private CardFakeShatterTuningAsset mTuningAsset;

    [Tooltip("内联调参（未指定 Asset 时生效）。所有字段均带中文说明。")]
    [SerializeField] private CardFakeShatterTuning mInlineTuning = CardFakeShatterTuning.CreateDefault();

    public static CardFakeShatterTuningProfile Instance { get; private set; }

    public CardFakeShatterTuning ActiveTuning =>
        mTuningAsset != null ? mTuningAsset.Tuning : mInlineTuning;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static CardFakeShatterTuningProfile FindInScene()
    {
        return Instance != null ? Instance : FindObjectOfType<CardFakeShatterTuningProfile>();
    }

    public CardFakeShatterSettings CreateRuntimeSettings(int battleStyleShardCount = 0)
    {
        var tuning = ActiveTuning ?? CardFakeShatterTuning.CreateDefault();
        var settings = tuning.ToRuntimeSettings();

        if (tuning.ScaleGridFromBattleStyle && battleStyleShardCount > 0)
        {
            CardFakeShatterSettings.ApplyGridFromShardCount(settings, battleStyleShardCount);
        }

        if (settings.EnableRuntimeVariance)
        {
            settings.ApplyRuntimeVariance();
        }

        return settings;
    }

#if UNITY_EDITOR
    [ContextMenu("Copy Active Tuning From Scene Defaults")]
    private void CopyFromCodeDefaults()
    {
        mInlineTuning = CardFakeShatterTuning.CreateDefault();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
