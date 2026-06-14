using UnityEngine;

public sealed class BakedCardWorldView : MonoBehaviour
{
    private const float PivotDepth = 0.01f;

    [SerializeField] private Transform mVisualPivot;
    [SerializeField] private SpriteRenderer mFaceRenderer;
    [SerializeField] private SpriteRenderer mBackRenderer;
    [SerializeField] private float mWorldHeight = 3f;
    [SerializeField] private bool mAnimateFlip = true;
    [SerializeField] private float mFlipSpeed = 38f;
    [SerializeField] private float mTiltAmplitude = 8f;

    private float mTimeOffset;
    private Sprite mRuntimeFaceSprite;

    public void Initialize(string sortingLayerName, int sortingOrder, float worldHeight)
    {
        mWorldHeight = worldHeight;
        EnsureRenderers();

        mFaceRenderer.sortingLayerName = sortingLayerName;
        mBackRenderer.sortingLayerName = sortingLayerName;
        mFaceRenderer.sortingOrder = sortingOrder;
        mBackRenderer.sortingOrder = sortingOrder;
        mTimeOffset = Random.value * 10f;
    }

    public void SetSprites(Sprite faceSprite, Sprite backSprite)
    {
        EnsureRenderers();
        ReleaseRuntimeFaceSprite();
        mRuntimeFaceSprite = faceSprite;
        mFaceRenderer.sprite = faceSprite;
        mBackRenderer.sprite = backSprite;
        FitToHeight(mFaceRenderer);
        FitToHeight(mBackRenderer);
        UpdateFaceSide();
    }

    private void Awake()
    {
        EnsureRenderers();
    }

    private void Update()
    {
        if (mAnimateFlip && mVisualPivot != null)
        {
            var t = Time.time + mTimeOffset;
            mVisualPivot.localRotation = Quaternion.Euler(
                Mathf.Sin(t * 1.15f) * mTiltAmplitude,
                t * mFlipSpeed,
                Mathf.Sin(t * 0.7f) * 4f);
        }

        UpdateFaceSide();
    }

    private void OnDestroy()
    {
        ReleaseRuntimeFaceSprite();
    }

    private void EnsureRenderers()
    {
        if (mVisualPivot == null)
        {
            var pivotObject = new GameObject("VisualPivot");
            pivotObject.transform.SetParent(transform, false);
            mVisualPivot = pivotObject.transform;
        }

        if (mFaceRenderer == null)
        {
            mFaceRenderer = CreateRenderer("Face", -PivotDepth);
        }

        if (mBackRenderer == null)
        {
            mBackRenderer = CreateRenderer("Back", PivotDepth);
            mBackRenderer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }

    private SpriteRenderer CreateRenderer(string name, float localZ)
    {
        var rendererObject = new GameObject(name);
        rendererObject.transform.SetParent(mVisualPivot, false);
        rendererObject.transform.localPosition = new Vector3(0f, 0f, localZ);
        var spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        return spriteRenderer;
    }

    private void FitToHeight(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        var height = spriteRenderer.sprite.bounds.size.y;
        if (height <= 0f)
        {
            return;
        }

        var scale = mWorldHeight / height;
        spriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void UpdateFaceSide()
    {
        BakedCardFaceVisibility.Apply(mVisualPivot, mFaceRenderer, mBackRenderer);
    }

    private void ReleaseRuntimeFaceSprite()
    {
        BakedCardRuntimeSpriteUtility.Release(ref mRuntimeFaceSprite);
    }
}
