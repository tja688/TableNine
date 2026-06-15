using UnityEngine;

/// <summary>
/// 可复用的卡牌碎裂调参资产。菜单：Create → TableNine → Card Fake Shatter Tuning。
/// </summary>
[CreateAssetMenu(fileName = "CardFakeShatterTuning", menuName = "TableNine/Card Fake Shatter Tuning")]
public sealed class CardFakeShatterTuningAsset : ScriptableObject
{
    [Tooltip("碎裂参数。修改后保存资产即可，场景中引用该资产的 Profile 会自动使用。")]
    public CardFakeShatterTuning Tuning = CardFakeShatterTuning.CreateDefault();
}
