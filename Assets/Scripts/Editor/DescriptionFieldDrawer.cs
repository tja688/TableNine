#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DescriptionFieldAttribute))]
public sealed class DescriptionFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);
        var text = property.stringValue ?? string.Empty;
        var lineHeight = EditorGUIUtility.singleLineHeight;
        var fieldRect = new Rect(position.x, position.y, position.width, lineHeight);
        var counterRect = new Rect(position.x, position.y + lineHeight + 2f, position.width, lineHeight);

        EditorGUI.PropertyField(fieldRect, property, label);

        var length = DescriptionPanelTextRules.CountLength(text);
        var counterStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = length > DescriptionPanelTextRules.MaxLength ? Color.red : Color.gray }
        };
        EditorGUI.LabelField(counterRect, $"{length}/{DescriptionPanelTextRules.MaxLength}", counterStyle);

        if (DescriptionPanelTextRules.ContainsLineBreak(text)
            || length > DescriptionPanelTextRules.MaxLength)
        {
            var sanitized = DescriptionPanelTextRules.SanitizeForStorage(text, out _);
            if (!string.Equals(sanitized, text, System.StringComparison.Ordinal))
            {
                property.stringValue = sanitized;
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + 4f;
    }
}
#endif
