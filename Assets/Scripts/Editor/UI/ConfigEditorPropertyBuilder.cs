#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

public static class ConfigEditorPropertyBuilder
{
    public static void AddProperty(
        VisualElement parent,
        WarmConsoleUiSkin skin,
        SerializedObject so,
        SerializedProperty property,
        bool readOnly = false)
    {
        if (property == null)
        {
            return;
        }

        if (property.name == "Description")
        {
            var descField = new DescriptionFieldElement(skin);
            descField.Bind(so, property);
            parent.Add(descField);
            return;
        }

        if (property.name == "Image")
        {
            return;
        }

        var (label, desc) = ConfigEditorFieldLabels.Get(property);
        parent.Add(skin.WrapProperty(label, desc, property, so, readOnly));
    }

    public static void AddProperties(
        VisualElement parent,
        WarmConsoleUiSkin skin,
        SerializedObject so,
        SerializedProperty parentProperty,
        params string[] propertyNames)
    {
        for (var i = 0; i < propertyNames.Length; i++)
        {
            AddProperty(parent, skin, so, parentProperty.FindPropertyRelative(propertyNames[i]));
        }
    }

    public static void AddAllChildrenExcept(
        VisualElement parent,
        WarmConsoleUiSkin skin,
        SerializedObject so,
        SerializedProperty parentProperty,
        params string[] exclude)
    {
        var excludeSet = new System.Collections.Generic.HashSet<string>(exclude);
        var iterator = parentProperty.Copy();
        var end = iterator.GetEndProperty();
        var enterChildren = true;

        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            if (iterator.depth != parentProperty.depth + 1)
            {
                enterChildren = false;
                continue;
            }

            enterChildren = false;
            if (excludeSet.Contains(iterator.name))
            {
                continue;
            }

            AddProperty(parent, skin, so, iterator.Copy());
        }
    }
}
#endif
