#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DescriptionPanelConfigValidator
{
    public static void SanitizeAsset(ScriptableObject asset)
    {
        if (asset == null)
        {
            return;
        }

        var assetPath = AssetDatabase.GetAssetPath(asset);
        switch (asset)
        {
            case TableNineCharacterConfig characterConfig:
                DescriptionPanelConfigValidation.SanitizeCharacterConfig(characterConfig);
                LogAssetPath(assetPath, "Character");
                break;
            case TableNineCardConfig cardConfig:
                DescriptionPanelConfigValidation.SanitizeCardConfig(cardConfig);
                LogAssetPath(assetPath, "Card");
                break;
            case TableNineSkillConfig skillConfig:
                DescriptionPanelConfigValidation.SanitizeSkillConfig(skillConfig);
                LogAssetPath(assetPath, "Skill");
                break;
            case TableNineRelicConfig relicConfig:
                DescriptionPanelConfigValidation.SanitizeRelicConfig(relicConfig);
                LogAssetPath(assetPath, "Relic/Room");
                break;
        }

        if (!string.IsNullOrEmpty(assetPath))
        {
            EditorUtility.SetDirty(asset);
        }
    }

    private static void LogAssetPath(string assetPath, string kind)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        Debug.Log($"[DescriptionPanelConfig] Sanitized {kind} descriptions in {assetPath}");
    }
}
#endif
