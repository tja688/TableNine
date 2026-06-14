using QFramework;
using UnityEngine;

public sealed class CardDisplayAdapter : MonoBehaviour
{
    private const float PivotDepth = 0.01f;

    [SerializeField] private Transform mVisualPivot;
    [SerializeField] private SpriteRenderer mFaceRenderer;
    [SerializeField] private SpriteRenderer mBackRenderer;
    [SerializeField] private BoxCollider2D mCollider;
    [SerializeField] private float mTargetWorldHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight;

    private Vector3 mInitialLocalScale = Vector3.one;
    private Sprite mRuntimeFaceSprite;
    private Sprite mRuntimeBackSprite;
    private bool? mForceFaceVisible;
    private float mContentFillRatio = 1f;

    public float TargetWorldHeight => mTargetWorldHeight;
    public Transform VisualPivot => mVisualPivot;

    public void Initialize()
    {
        EnsureHierarchy();
        if (mCollider != null)
        {
            mCollider.isTrigger = true;
        }

        mInitialLocalScale = transform.localScale;
    }

    public void SetTargetWorldHeight(float targetWorldHeight)
    {
        mTargetWorldHeight = Mathf.Max(0.1f, targetWorldHeight);
    }

    public void SetFaceVisible(bool faceVisible)
    {
        mForceFaceVisible = faceVisible;
        UpdateFaceVisibility();
    }

    public void ClearFaceVisibilityOverride()
    {
        mForceFaceVisible = null;
        UpdateFaceVisibility();
    }

    public void ApplySprites(BakedCardSpriteSet sprites, bool pending = false)
    {
        gameObject.SetActive(true);
        ReleaseRuntimeSprites();

        mRuntimeFaceSprite = sprites.FaceSprite;
        mRuntimeBackSprite = sprites.BackSprite;
        mContentFillRatio = sprites.ContentFillRatio > 0f ? sprites.ContentFillRatio : 1f;

        if (mFaceRenderer != null)
        {
            mFaceRenderer.sprite = sprites.FaceSprite;
            mFaceRenderer.color = pending ? new Color(1f, 0.97f, 0.88f) : Color.white;
        }

        if (mBackRenderer != null)
        {
            mBackRenderer.sprite = sprites.BackSprite;
            if (mFaceRenderer != null)
            {
                mBackRenderer.sortingLayerID = mFaceRenderer.sortingLayerID;
                mBackRenderer.sortingOrder = mFaceRenderer.sortingOrder;
            }
        }

        FitToWorldHeight(mTargetWorldHeight, mContentFillRatio);
        SyncCollider(sprites.FaceSprite);
        UpdateFaceVisibility();
    }

    public void ApplyBaked(
        IController controller,
        CardViewData data,
        BakedCardFaceComposer composer,
        bool pending = false)
    {
        if (controller == null || data == null || composer == null)
        {
            return;
        }

        var renderData = BakedCardRenderDataFactory.CreateRuntime(controller, data);
        if (renderData == null)
        {
            return;
        }

        var sprites = composer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
        if (!sprites.HasFace)
        {
            return;
        }

        ApplySprites(sprites, pending);
    }

    public void Hide()
    {
        ReleaseRuntimeSprites();
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        UpdateFaceVisibility();
    }

    private void OnDestroy()
    {
        ReleaseRuntimeSprites();
    }

    private void EnsureHierarchy()
    {
        if (mCollider == null)
        {
            mCollider = GetComponent<BoxCollider2D>();
        }

        if (mVisualPivot == null)
        {
            var existingPivot = transform.Find("VisualPivot");
            mVisualPivot = existingPivot != null ? existingPivot : CreateChildTransform("VisualPivot", transform).transform;
        }

        if (mFaceRenderer == null)
        {
            var faceTransform = mVisualPivot.Find("Face");
            mFaceRenderer = faceTransform != null
                ? faceTransform.GetComponent<SpriteRenderer>()
                : CreateRenderer("Face", mVisualPivot, -PivotDepth);
        }

        if (mBackRenderer == null)
        {
            var backTransform = mVisualPivot.Find("Back");
            if (backTransform != null)
            {
                mBackRenderer = backTransform.GetComponent<SpriteRenderer>();
            }
            else
            {
                mBackRenderer = CreateRenderer("Back", mVisualPivot, PivotDepth);
                mBackRenderer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }
    }

    private static GameObject CreateChildTransform(string name, Transform parent)
    {
        var childObject = new GameObject(name);
        childObject.transform.SetParent(parent, false);
        return childObject;
    }

    private static SpriteRenderer CreateRenderer(string name, Transform parent, float localZ)
    {
        var rendererObject = CreateChildTransform(name, parent);
        rendererObject.transform.localPosition = new Vector3(0f, 0f, localZ);
        var spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        return spriteRenderer;
    }

    private void FitToWorldHeight(float targetWorldHeight, float contentFillRatio)
    {
        if (mFaceRenderer == null || mFaceRenderer.sprite == null)
        {
            transform.localScale = mInitialLocalScale;
            return;
        }

        var localHeight = mFaceRenderer.sprite.bounds.size.y * Mathf.Max(0.1f, contentFillRatio);
        if (localHeight <= 0f)
        {
            return;
        }

        var parentScale = transform.parent != null ? transform.parent.lossyScale.y : 1f;
        if (Mathf.Approximately(parentScale, 0f))
        {
            parentScale = 1f;
        }

        var scale = targetWorldHeight / (localHeight * parentScale);
        transform.localScale = mInitialLocalScale * scale;
    }

    private void SyncCollider(Sprite sprite)
    {
        if (mCollider == null)
        {
            return;
        }

        if (sprite == null)
        {
            mCollider.size = new Vector2(1.5f, BakedCardRenderDataFactory.TemplateLocalCardHeight);
            mCollider.offset = Vector2.zero;
            return;
        }

        mCollider.size = sprite.bounds.size;
        mCollider.offset = (Vector2)sprite.bounds.center;
    }

    private void UpdateFaceVisibility()
    {
        BakedCardFaceVisibility.Apply(mVisualPivot, mFaceRenderer, mBackRenderer, mForceFaceVisible);
    }

    private void ReleaseRuntimeSprites()
    {
        BakedCardRuntimeSpriteUtility.Release(ref mRuntimeFaceSprite);
        BakedCardRuntimeSpriteUtility.Release(ref mRuntimeBackSprite);
    }
}
