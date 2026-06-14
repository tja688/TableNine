using UnityEngine;

public sealed class CardStackWorldCard : MonoBehaviour
{
    private CardStackWorldDemo mOwner;
    private CardView mCardView;
    private Collider2D mCollider;
    private Transform mDragLayer;
    private Transform mVisualPivot;
    private SpriteRenderer mFaceRenderer;
    private SpriteRenderer mBackRenderer;

    private float mRandomRotation;
    private int mBaseSortingOrder;
    private int mStackIndex;
    private int mStackCount;
    private Vector2 mPivotOffset;
    private float mDragThreshold;

    private Vector3 mDragOffset;
    private Vector3 mDragOffsetVelocity;
    private bool mDragging;

    private float mStackRotZ;
    private float mStackRotVelocity;
    private float mTargetStackRotZ;

    private float mStackScale = 1f;
    private float mStackScaleVelocity;
    private float mTargetStackScale = 1f;

    private float mTiltX;
    private float mTiltXVelocity;
    private float mTiltY;
    private float mTiltYVelocity;

    public Transform DragLayer => mDragLayer;
    public Vector3 DragOffset => mDragOffset;

    public void Initialize(
        CardStackWorldDemo owner,
        CardView cardView,
        float randomRotation,
        int baseSortingOrder)
    {
        mOwner = owner;
        mCardView = cardView;
        mRandomRotation = randomRotation;
        mBaseSortingOrder = baseSortingOrder;

        mDragLayer = cardView.transform.parent;
        mCollider = cardView.GetComponent<Collider2D>();
        mVisualPivot = cardView.DisplayAdapter != null ? cardView.DisplayAdapter.VisualPivot : cardView.transform;
        CacheRenderers();
        CachePivotAndThreshold();
    }

    public void SetStackIndex(int stackIndex, int stackCount)
    {
        mStackIndex = stackIndex;
        mStackCount = stackCount;
        mTargetStackRotZ = (stackCount - stackIndex - 1) * 4f + mRandomRotation;
        mTargetStackScale = 1f + stackIndex * 0.06f - stackCount * 0.06f;
        ApplySortingOrder();
    }

    public void BeginDrag()
    {
        mDragging = true;
    }

    public void SetDragTarget(Vector3 worldTarget)
    {
        if (!mDragging)
        {
            return;
        }

        var desiredOffset = worldTarget - transform.position;
        var maxOffset = mDragThreshold * 0.35f;
        if (desiredOffset.magnitude > maxOffset && maxOffset > 0f)
        {
            desiredOffset = desiredOffset.normalized * (maxOffset + (desiredOffset.magnitude - maxOffset) * mOwner.DragElastic);
        }

        mDragOffset = desiredOffset;
        var normalizedX = Mathf.Clamp(desiredOffset.x / Mathf.Max(0.01f, mDragThreshold), -1f, 1f);
        var normalizedY = Mathf.Clamp(desiredOffset.y / Mathf.Max(0.01f, mDragThreshold), -1f, 1f);
        mTiltX = SpringMath.Step(ref mTiltX, ref mTiltXVelocity, -normalizedY * 60f, mOwner.SpringStiffness, mOwner.SpringDamping, Time.deltaTime);
        mTiltY = SpringMath.Step(ref mTiltY, ref mTiltYVelocity, normalizedX * 60f, mOwner.SpringStiffness, mOwner.SpringDamping, Time.deltaTime);
    }

    public void EndDrag()
    {
        mDragging = false;
    }

    public bool ShouldSendToBack()
    {
        return mDragOffset.magnitude >= mDragThreshold;
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        return mCollider != null && mCollider.OverlapPoint(worldPoint);
    }

    public bool TryGetDragPlanePoint(Camera camera, out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (camera == null)
        {
            return false;
        }

        var screenPoint = Input.mousePosition;
        screenPoint.z = Mathf.Abs(camera.transform.position.z - transform.position.z);
        worldPoint = camera.ScreenToWorldPoint(screenPoint);
        worldPoint.z = transform.position.z;
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (mDragging)
        {
            mDragLayer.position = transform.position + mDragOffset;
        }
        else
        {
            mDragOffset.x = SpringMath.Step(ref mDragOffset.x, ref mDragOffsetVelocity.x, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            mDragOffset.y = SpringMath.Step(ref mDragOffset.y, ref mDragOffsetVelocity.y, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            mDragOffset.z = 0f;
            mDragLayer.position = transform.position + mDragOffset;
            mTiltX = SpringMath.Step(ref mTiltX, ref mTiltXVelocity, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            mTiltY = SpringMath.Step(ref mTiltY, ref mTiltYVelocity, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
        }

        mStackRotZ = SpringMath.Step(ref mStackRotZ, ref mStackRotVelocity, mTargetStackRotZ, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
        mStackScale = SpringMath.Step(ref mStackScale, ref mStackScaleVelocity, mTargetStackScale, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
        ApplyVisualTransform();
    }

    private void ApplyVisualTransform()
    {
        if (mVisualPivot == null)
        {
            return;
        }

        mDragLayer.localRotation = Quaternion.Euler(mTiltX, mTiltY, 0f);

        var rotation = Quaternion.Euler(0f, 0f, mStackRotZ);
        var scaledPivot = (Vector3)(mPivotOffset * mStackScale);
        mVisualPivot.localRotation = rotation;
        mVisualPivot.localScale = Vector3.one * mStackScale;
        mVisualPivot.localPosition = scaledPivot + rotation * -scaledPivot;
    }

    private void ApplySortingOrder()
    {
        var order = mBaseSortingOrder + mStackIndex;
        if (mFaceRenderer != null)
        {
            mFaceRenderer.sortingOrder = order;
        }

        if (mBackRenderer != null)
        {
            mBackRenderer.sortingOrder = order;
        }
    }

    private void CacheRenderers()
    {
        if (mVisualPivot == null)
        {
            return;
        }

        var face = mVisualPivot.Find("Face");
        if (face != null)
        {
            mFaceRenderer = face.GetComponent<SpriteRenderer>();
        }

        var back = mVisualPivot.Find("Back");
        if (back != null)
        {
            mBackRenderer = back.GetComponent<SpriteRenderer>();
        }
    }

    private void CachePivotAndThreshold()
    {
        if (mCollider != null)
        {
            var size = mCollider.bounds.size;
            mPivotOffset = new Vector2(size.x * 0.4f, -size.y * 0.4f);
            mDragThreshold = size.y * (mOwner.DragSensitivity / 100f);
            return;
        }

        mPivotOffset = new Vector2(0.6f, -0.9f);
        mDragThreshold = mOwner.DragSensitivity / 100f * 2f;
    }
}

internal static class SpringMath
{
    public static float Step(
        ref float current,
        ref float velocity,
        float target,
        float stiffness,
        float damping,
        float deltaTime)
    {
        var displacement = current - target;
        var acceleration = -stiffness * displacement - damping * velocity;
        velocity += acceleration * deltaTime;
        current += velocity * deltaTime;
        return current;
    }
}
