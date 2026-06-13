#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class DescriptionFieldElement : VisualElement
{
    private readonly WarmConsoleUiSkin _skin;
    private readonly TextField _textField;
    private readonly Label _counterLabel;
    private SerializedProperty _property;
    private SerializedObject _serializedObject;

    public DescriptionFieldElement(WarmConsoleUiSkin skin)
    {
        _skin = skin;
        _textField = new TextField();
        _textField.RegisterValueChangedCallback(OnTextChanged);

        _counterLabel = skin.CreateTinyPathLabel("0/25");
        _counterLabel.style.marginTop = 4;

        Add(skin.WrapControl("描述文案", "说明面板展示用，最多 25 字，不可换行。", _textField));
        Add(_counterLabel);
    }

    public void Bind(SerializedObject so, SerializedProperty property)
    {
        _serializedObject = so;
        _property = property;
        _textField.SetValueWithoutNotify(_property?.stringValue ?? string.Empty);
        UpdateCounter(_property?.stringValue ?? string.Empty);
    }

    private void OnTextChanged(ChangeEvent<string> evt)
    {
        if (_property == null || _serializedObject == null)
        {
            return;
        }

        var sanitized = DescriptionPanelTextRules.SanitizeForStorage(evt.newValue, out _);
        if (!string.Equals(sanitized, evt.newValue))
        {
            _textField.SetValueWithoutNotify(sanitized);
        }

        _property.stringValue = sanitized;
        _serializedObject.ApplyModifiedProperties();
        UpdateCounter(sanitized);
    }

    private void UpdateCounter(string text)
    {
        var length = DescriptionPanelTextRules.CountLength(text);
        _counterLabel.text = $"{length}/{DescriptionPanelTextRules.MaxLength}";
        _counterLabel.style.color = length > DescriptionPanelTextRules.MaxLength
            ? Color.red
            : _skin.Theme.TextPath;
    }
}
#endif
