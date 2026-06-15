using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单侧栏：面板滑入 + 菜单项文字微抬头 + Socials 渐显；点击面板外区域收回。
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseMenuView : MonoBehaviour
{
    [Serializable]
    public sealed class MenuItemBinding
    {
        public RectTransform Wrapper;
        public RectTransform LabelRect;
    }

    [Header("Layout")]
    [SerializeField] private bool mOpenOnRight = true;
    [SerializeField] private float mPanelWidth = 162f;

    [Header("Refs")]
    [SerializeField] private RectTransform mPanel;
    [SerializeField] private MenuItemBinding[] mMenuItems;
    [SerializeField] private CanvasGroup mSocialsGroup;
    [SerializeField] private Graphic mClickAwayBlocker;

    public bool IsOpen => mIsOpen;

    private bool mIsOpen;
    private bool mBusy;
    private Sequence mOpenSequence;
    private Tween mCloseTween;

    private void Awake()
    {
        AutoBind();
        ApplyClosedStateImmediate();
        ConfigureClickAwayFromBlocker();
    }

    private void OnDestroy()
    {
        KillTweens();
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
        SetClickAwayActive(true);
        PlayOpen();
    }

    public void Close()
    {
        if (!mIsOpen || mBusy)
        {
            return;
        }

        mIsOpen = false;
        SetClickAwayActive(false);
        PlayClose();
    }

    private void AutoBind()
    {
        mPanel = mPanel != null ? mPanel : FindDeep(transform, "MenuPanel") as RectTransform;
        mSocialsGroup = mSocialsGroup != null
            ? mSocialsGroup
            : FindDeep(transform, "MenuPanel/PanelInner/Socials")?.GetComponent<CanvasGroup>();

        if (mMenuItems == null || mMenuItems.Length == 0)
        {
            mMenuItems = CollectMenuItems();
        }

        EnsureClickAwayBlocker();
    }

    private MenuItemBinding[] CollectMenuItems()
    {
        var menuItemsRoot = FindDeep(transform, "MenuPanel/PanelInner/MenuItems");
        if (menuItemsRoot == null)
        {
            return Array.Empty<MenuItemBinding>();
        }

        var bindings = new MenuItemBinding[menuItemsRoot.childCount];
        var count = 0;
        for (var i = 0; i < menuItemsRoot.childCount; i++)
        {
            var wrapper = menuItemsRoot.GetChild(i) as RectTransform;
            if (wrapper == null)
            {
                continue;
            }

            var labelRect = wrapper.Find("LabelRect") as RectTransform;
            if (labelRect == null)
            {
                continue;
            }

            bindings[count++] = new MenuItemBinding
            {
                Wrapper = wrapper,
                LabelRect = labelRect
            };
        }

        if (count == bindings.Length)
        {
            return bindings;
        }

        Array.Resize(ref bindings, count);
        return bindings;
    }

    private void EnsureClickAwayBlocker()
    {
        if (mClickAwayBlocker != null)
        {
            return;
        }

        var existing = transform.Find("ClickAwayBlocker");
        if (existing != null)
        {
            mClickAwayBlocker = existing.GetComponent<Graphic>();
            return;
        }

        var blockerObject = new GameObject("ClickAwayBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        blockerObject.layer = gameObject.layer;
        var rectTransform = blockerObject.GetComponent<RectTransform>();
        rectTransform.SetParent(transform, false);
        rectTransform.SetAsFirstSibling();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        var image = blockerObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;
        mClickAwayBlocker = image;
    }

    private void ConfigureClickAwayFromBlocker()
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
        if (mIsOpen)
        {
            Close();
        }
    }

    private void SetClickAwayActive(bool active)
    {
        if (mClickAwayBlocker != null)
        {
            mClickAwayBlocker.raycastTarget = active;
        }

        SetPanelRaycast(active);
    }

    private void SetPanelRaycast(bool blocksRaycasts)
    {
        if (mPanel == null)
        {
            return;
        }

        var panelImage = mPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.raycastTarget = blocksRaycasts;
        }
    }

    private void ApplyClosedStateImmediate()
    {
        KillTweens();
        SetSlideX(mPanel, GetOffscreenX());
        ResetMenuItemPose();
        ResetSocialPose();
        SetClickAwayActive(false);
        SetPanelRaycast(false);
        mIsOpen = false;
        mBusy = false;
    }

    private float GetOffscreenX()
    {
        var width = mPanel != null && mPanel.rect.width > 1f ? mPanel.rect.width : mPanelWidth;
        return mOpenOnRight ? width : -width;
    }

    private static void SetSlideX(RectTransform target, float x)
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

    private void ResetSocialPose()
    {
        if (mSocialsGroup == null)
        {
            return;
        }

        mSocialsGroup.alpha = 0f;
    }

    private void PlayOpen()
    {
        mBusy = true;
        KillTweens();

        var offscreen = GetOffscreenX();
        SetSlideX(mPanel, offscreen);
        ResetMenuItemPose();
        ResetSocialPose();

        mOpenSequence = DOTween.Sequence();
        const float panelDuration = 0.65f;
        mOpenSequence.Insert(0f, TweenAnchorPosX(mPanel, 0f, panelDuration, Ease.OutQuart));

        var itemsStart = panelDuration * 0.15f;
        if (mMenuItems != null)
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
        }

        if (mSocialsGroup != null)
        {
            var socialsStart = panelDuration * 0.4f;
            mOpenSequence.Insert(
                socialsStart,
                DOTween.To(() => mSocialsGroup.alpha, value => mSocialsGroup.alpha = value, 1f, 0.5f)
                    .SetEase(Ease.OutQuad));
        }

        mOpenSequence.OnComplete(() => mBusy = false);
        mOpenSequence.Play();
    }

    private void PlayClose()
    {
        mBusy = true;
        KillTweens();

        var offscreen = GetOffscreenX();
        mCloseTween = TweenAnchorPosX(mPanel, offscreen, 0.32f, Ease.InCubic)
            .OnComplete(() =>
            {
                SetSlideX(mPanel, offscreen);
                ResetMenuItemPose();
                ResetSocialPose();
                mBusy = false;
            });
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

    private void KillTweens()
    {
        mOpenSequence?.Kill();
        mOpenSequence = null;
        mCloseTween?.Kill();
        mCloseTween = null;
    }

    private static Transform FindDeep(Transform root, string childPath)
    {
        if (root == null || string.IsNullOrWhiteSpace(childPath))
        {
            return null;
        }

        var segments = childPath.Split('/');
        var current = root;
        for (var i = 0; i < segments.Length; i++)
        {
            current = current.Find(segments[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }
}
