using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// React Bits StaggeredMenu 动效 Unity 复刻（DOTween）。
/// 文字子对象为可点击占位，业务逻辑与配色由外部自行对接。
/// </summary>
[DisallowMultipleComponent]
public sealed class StaggeredMenuView : MonoBehaviour
{
    [Serializable]
    public sealed class MenuItemBinding
    {
        public RectTransform Wrapper;
        public RectTransform LabelRect;
        public TMP_Text Label;
        public TMP_Text Number;
    }

    [Header("Layout")]
    [SerializeField] private bool mOpenOnRight = true;
    [SerializeField] private float mPanelWidth = 162f;

    [Header("Colors")]
    [SerializeField] private Color[] mPreLayerColors =
    {
        new Color32(0xB4, 0x97, 0xCF, 0xFF),
        new Color32(0x52, 0x27, 0xFF, 0xFF)
    };

    [SerializeField] private Color mMenuButtonColor = Color.white;
    [SerializeField] private Color mOpenMenuButtonColor = Color.white;
    [SerializeField] private Color mAccentColor = new Color32(0xFF, 0x6B, 0x6B, 0xFF);
    [SerializeField] private bool mChangeMenuColorOnOpen = true;

    [Header("Refs")]
    [SerializeField] private RectTransform mPreLayersRoot;
    [SerializeField] private RectTransform[] mPreLayers;
    [SerializeField] private RectTransform mPanel;
    [SerializeField] private RectTransform mToggleButton;
    [SerializeField] private RectTransform mToggleTextInner;
    [SerializeField] private RectTransform mToggleIcon;
    [SerializeField] private TMP_Text mToggleLabelTemplate;
    [SerializeField] private MenuItemBinding[] mMenuItems;
    [SerializeField] private TMP_Text mSocialsTitle;
    [SerializeField] private TMP_Text[] mSocialLinks;
    [SerializeField] private Graphic mClickAwayBlocker;
    [SerializeField] private TMP_Text mLogoText;

    [Header("Options")]
    [SerializeField] private bool mDisplayItemNumbering = true;
    [SerializeField] private bool mDisplaySocials = true;
    [SerializeField] private bool mCloseOnClickAway = false;

    [Header("Debug")]
    [SerializeField] private bool mEnableDebugKeyToggle = true;
    [SerializeField] private KeyCode mDebugToggleKey = KeyCode.Q;

    public bool IsOpen => mIsOpen;
    public event Action Opened;
    public event Action Closed;

    private bool mIsOpen;
    private bool mBusy;
    private Sequence mOpenSequence;
    private Tween mCloseTween;

    private void Awake()
    {
        ApplyClosedStateImmediate();
        ConfigureClickAwayFromBlocker();
        if (mClickAwayBlocker != null)
        {
            mClickAwayBlocker.raycastTarget = false;
        }

        var header = transform.Find("Header");
        if (header != null)
        {
            header.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    private void Update()
    {
        if (!mEnableDebugKeyToggle || mDebugToggleKey == KeyCode.None)
        {
            return;
        }

        if (Input.GetKeyDown(mDebugToggleKey))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (mBusy)
        {
            return;
        }

        if (mIsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (mIsOpen || mBusy)
        {
            return;
        }

        mIsOpen = true;
        if (mClickAwayBlocker != null)
        {
            mClickAwayBlocker.raycastTarget = mCloseOnClickAway;
        }

        PlayOpen();
        Opened?.Invoke();
    }

    public void Close()
    {
        if (!mIsOpen)
        {
            return;
        }

        mIsOpen = false;
        if (mClickAwayBlocker != null)
        {
            mClickAwayBlocker.raycastTarget = false;
        }

        PlayClose();
        Closed?.Invoke();
    }

    private void ApplyClosedStateImmediate()
    {
        KillTweens();

        var offscreen = GetOffscreenX();
        SetSlideX(mPreLayers, offscreen);
        if (mPanel != null)
        {
            SetSlideX(mPanel, offscreen);
        }

        ResetMenuItemPose();
        ResetSocialPose();
        ResetNumberOpacity();
    }

    private float GetOffscreenX()
    {
        return mOpenOnRight ? mPanelWidth : -mPanelWidth;
    }

    private static void SetSlideX(IReadOnlyList<RectTransform> targets, float x)
    {
        if (targets == null)
        {
            return;
        }

        for (var i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
            {
                continue;
            }

            var pos = targets[i].anchoredPosition;
            pos.x = x;
            targets[i].anchoredPosition = pos;
        }
    }

    private void SetSlideX(RectTransform target, float x)
    {
        if (target == null)
        {
            return;
        }

        var pos = target.anchoredPosition;
        pos.x = x;
        target.anchoredPosition = pos;
    }

    private void ResetMenuItemPose()
    {
        if (mMenuItems == null)
        {
            return;
        }

        for (var i = 0; i < mMenuItems.Length; i++)
        {
            var item = mMenuItems[i];
            if (item?.LabelRect == null)
            {
                continue;
            }

            var height = item.Wrapper != null ? item.Wrapper.rect.height : 28f;
            item.LabelRect.anchoredPosition = new Vector2(0f, -height * 1.4f);
            item.LabelRect.localRotation = Quaternion.Euler(0f, 0f, 10f);
        }
    }

    private void ResetNumberOpacity()
    {
        if (!mDisplayItemNumbering || mMenuItems == null)
        {
            return;
        }

        for (var i = 0; i < mMenuItems.Length; i++)
        {
            var number = mMenuItems[i]?.Number;
            if (number == null)
            {
                continue;
            }

            var color = number.color;
            color.a = 0f;
            number.color = color;
        }
    }

    private void ResetSocialPose()
    {
        if (mSocialsTitle != null)
        {
            var c = mSocialsTitle.color;
            c.a = 0f;
            mSocialsTitle.color = c;
        }

        if (mSocialLinks == null)
        {
            return;
        }

        for (var i = 0; i < mSocialLinks.Length; i++)
        {
            var link = mSocialLinks[i];
            if (link == null)
            {
                continue;
            }

            var color = link.color;
            color.a = 0f;
            link.color = color;
            var rt = link.rectTransform;
            var pos = rt.anchoredPosition;
            pos.y = 25f;
            rt.anchoredPosition = pos;
        }
    }

    private void PlayOpen()
    {
        mBusy = true;
        KillTweens();

        var offscreen = GetOffscreenX();
        SetSlideX(mPreLayers, offscreen);
        SetSlideX(mPanel, offscreen);
        ResetMenuItemPose();
        ResetSocialPose();
        ResetNumberOpacity();

        mOpenSequence = DOTween.Sequence();

        var layerCount = mPreLayers != null ? mPreLayers.Length : 0;
        for (var i = 0; i < layerCount; i++)
        {
            var layer = mPreLayers[i];
            if (layer == null)
            {
                continue;
            }

            mOpenSequence.Insert(
                i * 0.07f,
                TweenAnchorPosX(layer, 0f, 0.5f, Ease.OutQuart));
        }

        var lastLayerTime = layerCount > 0 ? (layerCount - 1) * 0.07f : 0f;
        var panelInsertTime = lastLayerTime + (layerCount > 0 ? 0.08f : 0f);
        const float panelDuration = 0.65f;

        if (mPanel != null)
        {
            mOpenSequence.Insert(panelInsertTime, TweenAnchorPosX(mPanel, 0f, panelDuration, Ease.OutQuart));
        }

        var itemsStart = panelInsertTime + panelDuration * 0.15f;
        if (mMenuItems != null && mMenuItems.Length > 0)
        {
            for (var i = 0; i < mMenuItems.Length; i++)
            {
                var item = mMenuItems[i];
                if (item?.LabelRect == null)
                {
                    continue;
                }

                var at = itemsStart + i * 0.1f;
                mOpenSequence.Insert(at, TweenAnchorPosY(item.LabelRect, 0f, 1f, Ease.OutQuart));
                mOpenSequence.Insert(at, TweenLocalEulerZ(item.LabelRect, 0f, 1f, Ease.OutQuart));
            }

            if (mDisplayItemNumbering)
            {
                for (var i = 0; i < mMenuItems.Length; i++)
                {
                    var number = mMenuItems[i]?.Number;
                    if (number == null)
                    {
                        continue;
                    }

                    mOpenSequence.Insert(
                        itemsStart + 0.1f + i * 0.08f,
                        TweenTmpAlpha(number, 1f, 0.6f, Ease.OutQuad));
                }
            }
        }

        if (mDisplaySocials)
        {
            var socialsStart = panelInsertTime + panelDuration * 0.4f;
            if (mSocialsTitle != null)
            {
                mOpenSequence.Insert(socialsStart, TweenTmpAlpha(mSocialsTitle, 1f, 0.5f, Ease.OutQuad));
            }

            if (mSocialLinks != null)
            {
                for (var i = 0; i < mSocialLinks.Length; i++)
                {
                    var link = mSocialLinks[i];
                    if (link == null)
                    {
                        continue;
                    }

                    var rt = link.rectTransform;
                    var at = socialsStart + 0.04f + i * 0.08f;
                    mOpenSequence.Insert(at, TweenAnchorPosY(rt, 0f, 0.55f, Ease.OutCubic));
                    mOpenSequence.Insert(at, TweenTmpAlpha(link, 1f, 0.55f, Ease.OutCubic));
                }
            }
        }

        mOpenSequence.OnComplete(() => mBusy = false);
        mOpenSequence.Play();
    }

    private static Tween TweenAnchorPosX(RectTransform target, float endX, float duration, Ease ease)
    {
        return DOTween.To(
                () => target.anchoredPosition,
                value => target.anchoredPosition = value,
                new Vector2(endX, target.anchoredPosition.y),
                duration)
            .SetEase(ease);
    }

    private static Tween TweenAnchorPosY(RectTransform target, float endY, float duration, Ease ease)
    {
        return DOTween.To(
                () => target.anchoredPosition,
                value => target.anchoredPosition = value,
                new Vector2(target.anchoredPosition.x, endY),
                duration)
            .SetEase(ease);
    }

    private static Tween TweenLocalEulerZ(RectTransform target, float endZ, float duration, Ease ease)
    {
        return DOTween.To(
                () => target.localEulerAngles,
                value => target.localEulerAngles = value,
                new Vector3(0f, 0f, endZ),
                duration)
            .SetEase(ease);
    }

    private static Tween TweenTmpAlpha(TMP_Text text, float endAlpha, float duration, Ease ease)
    {
        var color = text.color;
        var endColor = new Color(color.r, color.g, color.b, endAlpha);
        return DOTween.To(() => text.color, value => text.color = value, endColor, duration).SetEase(ease);
    }

    private void PlayClose()
    {
        mBusy = true;
        KillTweens();

        var targets = new List<RectTransform>();
        if (mPreLayers != null)
        {
            for (var i = 0; i < mPreLayers.Length; i++)
            {
                if (mPreLayers[i] != null)
                {
                    targets.Add(mPreLayers[i]);
                }
            }
        }

        if (mPanel != null)
        {
            targets.Add(mPanel);
        }

        var offscreen = GetOffscreenX();
        var closeSequence = DOTween.Sequence();
        for (var i = 0; i < targets.Count; i++)
        {
            closeSequence.Join(TweenAnchorPosX(targets[i], offscreen, 0.32f, Ease.InCubic));
        }

        mCloseTween = closeSequence.OnComplete(() =>
        {
            SetSlideX(mPreLayers, offscreen);
            SetSlideX(mPanel, offscreen);
            ResetMenuItemPose();
            ResetSocialPose();
            ResetNumberOpacity();
            mBusy = false;
        });
    }

    private void KillTweens()
    {
        mOpenSequence?.Kill();
        mOpenSequence = null;
        mCloseTween?.Kill();
        mCloseTween = null;
    }

    public void ConfigureClickAwayFromBlocker()
    {
        if (mClickAwayBlocker == null)
        {
            return;
        }

        var click = mClickAwayBlocker.GetComponent<StaggeredMenuTextButton>();
        if (click == null)
        {
            click = mClickAwayBlocker.gameObject.AddComponent<StaggeredMenuTextButton>();
        }

        click.Clicked -= OnClickAway;
        click.Clicked += OnClickAway;
    }

    private void OnClickAway()
    {
        if (mCloseOnClickAway && mIsOpen)
        {
            Close();
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(
        RectTransform preLayersRoot,
        RectTransform[] preLayers,
        RectTransform panel,
        RectTransform toggleButton,
        RectTransform toggleTextInner,
        RectTransform toggleIcon,
        TMP_Text toggleLabelTemplate,
        MenuItemBinding[] menuItems,
        TMP_Text socialsTitle,
        TMP_Text[] socialLinks,
        Graphic clickAwayBlocker,
        TMP_Text logoText)
    {
        mPreLayersRoot = preLayersRoot;
        mPreLayers = preLayers;
        mPanel = panel;
        mToggleButton = toggleButton;
        mToggleTextInner = toggleTextInner;
        mToggleIcon = toggleIcon;
        mToggleLabelTemplate = toggleLabelTemplate;
        mMenuItems = menuItems;
        mSocialsTitle = socialsTitle;
        mSocialLinks = socialLinks;
        mClickAwayBlocker = clickAwayBlocker;
        mLogoText = logoText;
    }
#endif
}
