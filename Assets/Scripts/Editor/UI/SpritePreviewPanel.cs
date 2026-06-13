#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SpritePreviewPanel : VisualElement
{
    private readonly WarmConsoleUiSkin _skin;
    private VisualElement _previewBox;
    private Label _metaLabel;
    private SerializedProperty _imageProperty;

    public SpritePreviewPanel(WarmConsoleUiSkin skin)
    {
        _skin = skin;
        style.width = 240;
        style.backgroundColor = skin.Theme.SectionCardBg;
        style.borderTopLeftRadius = style.borderTopRightRadius = 8;
        style.borderBottomLeftRadius = style.borderBottomRightRadius = 8;
        style.paddingTop = 12;
        style.paddingBottom = 12;
        style.paddingLeft = 12;
        style.paddingRight = 12;
        style.overflow = Overflow.Hidden;
    }

    public void Bind(SerializedObject so, SerializedProperty imageProperty)
    {
        _imageProperty = imageProperty;
        Clear();
        Rebuild(so);
        RefreshPreview();
    }

    private void Rebuild(SerializedObject so)
    {
        var stripeRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var stripe = new VisualElement();
        stripe.style.width = 3;
        stripe.style.backgroundColor = _skin.Theme.AccentMid;
        stripe.style.marginRight = 10;
        stripeRow.Add(stripe);

        var body = new VisualElement { style = { flexGrow = 1 } };
        body.Add(_skin.CreateTitleLabel("图像预览", 13, true, _skin.Theme.TextPrimary));

        _previewBox = new VisualElement();
        _previewBox.style.height = 200;
        _previewBox.style.marginTop = 10;
        _previewBox.style.marginBottom = 10;
        _previewBox.style.backgroundColor = _skin.Theme.ContentBg;
        _previewBox.style.borderTopLeftRadius = _previewBox.style.borderTopRightRadius = 6;
        _previewBox.style.borderBottomLeftRadius = _previewBox.style.borderBottomRightRadius = 6;
        _previewBox.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        body.Add(_previewBox);

        _metaLabel = _skin.CreateTinyPathLabel("未指定 Sprite");
        _metaLabel.style.marginBottom = 8;
        body.Add(_metaLabel);

        if (_imageProperty != null)
        {
            var objectField = new ObjectField("Sprite")
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false
            };
            objectField.BindProperty(_imageProperty);
            objectField.Bind(so);
            objectField.RegisterValueChangedCallback(_ => RefreshPreview());
            body.Add(_skin.WrapControl("图像资源", "绑定展示用 Sprite。", objectField));
        }

        stripeRow.Add(body);
        Add(stripeRow);
    }

    private void RefreshPreview()
    {
        if (_previewBox == null || _metaLabel == null)
        {
            return;
        }

        Sprite sprite = null;
        if (_imageProperty != null)
        {
            sprite = _imageProperty.objectReferenceValue as Sprite;
        }

        if (sprite != null && sprite.texture != null)
        {
            _previewBox.style.backgroundImage = new StyleBackground(sprite);
            _metaLabel.text = $"{sprite.texture.width}×{sprite.texture.height} px · {sprite.name}";
            _metaLabel.style.color = _skin.Theme.TextPath;
        }
        else
        {
            _previewBox.style.backgroundImage = StyleKeyword.None;
            _metaLabel.text = "未指定 Sprite";
            _metaLabel.style.color = _skin.Theme.TextSecondary;
        }
    }
}
#endif
