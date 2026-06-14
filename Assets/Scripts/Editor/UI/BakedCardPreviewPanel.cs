#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class BakedCardPreviewPanel : VisualElement
{
    private readonly WarmConsoleUiSkin _skin;
    private readonly SerializedObject _serializedObject;
    private readonly Func<BakedCardFaceRenderData> _renderDataResolver;
    private readonly VisualElement _previewBox;
    private readonly Label _metaLabel;

    private Sprite _previewSprite;
    private Sprite _backSprite;

    public BakedCardPreviewPanel(
        WarmConsoleUiSkin skin,
        SerializedObject serializedObject,
        Func<BakedCardFaceRenderData> renderDataResolver)
    {
        _skin = skin;
        _serializedObject = serializedObject;
        _renderDataResolver = renderDataResolver;

        style.width = 240;
        style.backgroundColor = skin.Theme.SectionCardBg;
        style.borderTopLeftRadius = style.borderTopRightRadius = 8;
        style.borderBottomLeftRadius = style.borderBottomRightRadius = 8;
        style.paddingTop = 12;
        style.paddingBottom = 12;
        style.paddingLeft = 12;
        style.paddingRight = 12;
        style.overflow = Overflow.Hidden;

        var stripeRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var stripe = new VisualElement();
        stripe.style.width = 3;
        stripe.style.backgroundColor = skin.Theme.AccentMid;
        stripe.style.marginRight = 10;
        stripeRow.Add(stripe);

        var body = new VisualElement { style = { flexGrow = 1 } };
        body.Add(_skin.CreateTitleLabel("烘焙后预览", 13, true, _skin.Theme.TextPrimary));

        _previewBox = new VisualElement();
        _previewBox.style.height = 200;
        _previewBox.style.marginTop = 10;
        _previewBox.style.marginBottom = 10;
        _previewBox.style.backgroundColor = _skin.Theme.ContentBg;
        _previewBox.style.borderTopLeftRadius = _previewBox.style.borderTopRightRadius = 6;
        _previewBox.style.borderBottomLeftRadius = _previewBox.style.borderBottomRightRadius = 6;
        _previewBox.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        body.Add(_previewBox);

        _metaLabel = _skin.CreateTinyPathLabel("等待生成烘焙预览");
        body.Add(_metaLabel);

        stripeRow.Add(body);
        Add(stripeRow);

        this.TrackSerializedObjectValue(_serializedObject, _ => RefreshPreview());
        RegisterCallback<DetachFromPanelEvent>(_ => ReleasePreviewSprites());
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        ReleasePreviewSprites();

        var renderData = _renderDataResolver?.Invoke();
        if (renderData == null)
        {
            ShowEmpty("当前卡牌缺少可烘焙数据");
            return;
        }

        var composer = new BakedCardFaceComposer();
        try
        {
            _previewSprite = composer.Compose(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit, out _backSprite);
        }
        finally
        {
            composer.Dispose();
        }

        if (_previewSprite == null)
        {
            ShowEmpty("预览生成失败");
            return;
        }

        _previewBox.style.backgroundImage = new StyleBackground(_previewSprite);
        _metaLabel.text = $"{_previewSprite.texture.width}×{_previewSprite.texture.height} px · {renderData.Template}";
        _metaLabel.style.color = _skin.Theme.TextPath;
    }

    private void ShowEmpty(string message)
    {
        _previewBox.style.backgroundImage = StyleKeyword.None;
        _metaLabel.text = message;
        _metaLabel.style.color = _skin.Theme.TextSecondary;
    }

    private void ReleasePreviewSprites()
    {
        ReleasePreviewSprite(ref _previewSprite);
        ReleasePreviewSprite(ref _backSprite);
    }

    private static void ReleasePreviewSprite(ref Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        var texture = sprite.texture;
        if (!IsRuntimeGeneratedSprite(sprite, texture))
        {
            sprite = null;
            return;
        }

        if (texture != null)
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }

        UnityEngine.Object.DestroyImmediate(sprite);
        sprite = null;
    }

    private static bool IsRuntimeGeneratedSprite(Sprite sprite, Texture texture)
    {
        return sprite != null
               && sprite.name.StartsWith("BakedCardFace_")
               && texture != null
               && texture.name.StartsWith("BakedCardFace_");
    }
}
#endif
