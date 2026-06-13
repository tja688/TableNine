#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

public static class EffectConfigEditorUi
{
    public static void AddAtomBlock(
        VisualElement parent,
        WarmConsoleUiSkin skin,
        SerializedObject so,
        SerializedProperty atomProperty,
        int index)
    {
        var atomType = atomProperty.FindPropertyRelative("AtomType").stringValue;
        var displayName = EffectConfigDisplayCatalog.GetAtomDisplayName(atomType);
        var description = EffectConfigDisplayCatalog.GetAtomDescription(atomType);

        parent.Add(skin.CreateSectionCard(
            $"步骤 {index + 1}：{displayName}",
            string.IsNullOrEmpty(description) ? $"类型键：{atomType}" : description,
            section =>
            {
                ConfigEditorPropertyBuilder.AddProperty(section, skin, so, atomProperty.FindPropertyRelative("AtomType"));
                AddParameterRows(section, skin, so, atomProperty.FindPropertyRelative("Parameters"));
            }));
    }

    public static void AddParameterRows(
        VisualElement parent,
        WarmConsoleUiSkin skin,
        SerializedObject so,
        SerializedProperty parameters)
    {
        if (parameters == null || !parameters.isArray)
        {
            return;
        }

        if (parameters.arraySize == 0)
        {
            parent.Add(skin.CreateDescriptionLabel("此步骤无额外参数。"));
            return;
        }

        for (var i = 0; i < parameters.arraySize; i++)
        {
            var param = parameters.GetArrayElementAtIndex(i);
            var valueProp = param.FindPropertyRelative("Value");
            var key = param.FindPropertyRelative("Key").stringValue ?? string.Empty;
            var (label, desc) = EffectConfigDisplayCatalog.GetParameterLabel(key);
            var rowDesc = string.IsNullOrEmpty(desc) ? $"参数键：{key}" : desc;
            parent.Add(skin.WrapProperty(label, rowDesc, valueProp, so));
        }
    }
}
#endif
