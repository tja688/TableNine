using UnityEngine;

public sealed class CardStackWorldCard : MonoBehaviour
{
    private CardStackWorldDemo mOwner;
    private CardDefinition mDefinition;
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
    private bool mReturning;
    private float mReturnDuration;

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
    private bool mPlaced;

    public Transform DragLayer => mDragLayer;
    public Vector3 DragOffset => mDragOffset;
    public CardDefinition Definition => mDefinition;
    public bool IsPlaced => mPlaced;

    private bool UseStackMotion => !mPlaced && !mReturning;

    public void Initialize(
        CardStackWorldDemo owner,
        CardDefinition definition,
        CardView cardView,
        float randomRotation,
        int baseSortingOrder)
    {
        mOwner = owner;
        mDefinition = definition;
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
        mReturning = false;
    }

    public void SetDragTarget(Vector3 worldTarget, bool isOverLinkedSlot)
    {
        if (!mDragging)
        {
            return;
        }

        mDragOffset = worldTarget - transform.position;
        UpdateDragTilt(isOverLinkedSlot, Time.deltaTime);
    }

    private void UpdateDragTilt(bool isOverLinkedSlot, float deltaTime)
    {
        if (isOverLinkedSlot)
        {
            mTiltX = SpringMath.Step(ref mTiltX, ref mTiltXVelocity, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            mTiltY = SpringMath.Step(ref mTiltY, ref mTiltYVelocity, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            return;
        }

        var normalizedX = Mathf.Clamp(mDragOffset.x / Mathf.Max(0.01f, mDragThreshold), -1f, 1f);
        var normalizedY = Mathf.Clamp(mDragOffset.y / Mathf.Max(0.01f, mDragThreshold), -1f, 1f);
        mTiltX = SpringMath.Step(ref mTiltX, ref mTiltXVelocity, -normalizedY * 60f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
        mTiltY = SpringMath.Step(ref mTiltY, ref mTiltYVelocity, normalizedX * 60f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
    }

    public void PlaceOnSlot(Transform slotTransform, Transform placedRoot)
    {
        mPlaced = true;
        mDragging = false;
        mReturning = false;
        ResetMotionState();
        ResetStackVisualImmediate();

        transform.SetParent(placedRoot, true);
        transform.position = slotTransform.position;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        mDragLayer.localPosition = Vector3.zero;
        mDragLayer.localRotation = Quaternion.identity;
        ApplyFlatVisualTransform();
        ApplyPlacedSortingOrder();
    }

    public void BeginReturnToStack(Transform stackRoot, float duration)
    {
        mPlaced = false;
        mDragging = false;
        mReturning = true;
        mReturnDuration = Mathf.Max(0.05f, duration);
        ResetMotionState();
        ResetStackVisualImmediate();

        transform.SetParent(stackRoot, true);
        mDragLayer.localPosition = Vector3.zero;
        mDragLayer.localRotation = Quaternion.identity;
        ApplyFlatVisualTransform();
    }

    public void EndDrag()
    {
        mDragging = false;
        if (!UseStackMotion)
        {
            mDragOffset = Vector3.zero;
            mDragOffsetVelocity = Vector3.zero;
            mDragLayer.localPosition = Vector3.zero;
            mDragLayer.localRotation = Quaternion.identity;
        }
    }

    public bool ShouldSendToBack()
    {
        return UseStackMotion && mDragOffset.magnitude >= mDragThreshold;
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
        if (mReturning)
        {
            TickReturnToStack(deltaTime);
            return;
        }

        if (mPlaced && !mDragging)
        {
            return;
        }

        if (mDragging)
        {
            mDragLayer.position = transform.position + mDragOffset;
            if (UseStackMotion)
            {
                ApplyVisualTransform();
            }
            else
            {
                ApplyFlatVisualTransform();
                ApplyDragTilt();
            }

            return;
        }

        if (UseStackMotion)
        {
            mDragOffset.x = SpringMath.Step(ref mDragOffset.x, ref mDragOffsetVelocity.x, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            mDragOffset.y = SpringMath.Step(ref mDragOffset.y, ref mDragOffsetVelocity.y, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            mDragOffset.z = 0f;
            mDragLayer.position = transform.position + mDragOffset;
            mTiltX = SpringMath.Step(ref mTiltX, ref mTiltXVelocity, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            mTiltY = SpringMath.Step(ref mTiltY, ref mTiltYVelocity, 0f, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            mStackRotZ = SpringMath.Step(ref mStackRotZ, ref mStackRotVelocity, mTargetStackRotZ, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            mStackScale = SpringMath.Step(ref mStackScale, ref mStackScaleVelocity, mTargetStackScale, mOwner.SpringStiffness, mOwner.SpringDamping, deltaTime);
            ApplyVisualTransform();
        }
    }

    private void TickReturnToStack(float deltaTime)
    {
        var ease = 1f - Mathf.Exp(-(8f / mReturnDuration) * deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, ease);
        mStackRotZ = Mathf.LerpAngle(mStackRotZ, mTargetStackRotZ, ease);
        mStackScale = Mathf.Lerp(mStackScale, mTargetStackScale, ease);
        ApplyVisualTransform();

        if (transform.localPosition.sqrMagnitude <= 0.0004f)
        {
            transform.localPosition = Vector3.zero;
            mReturning = false;
            mStackRotZ = mTargetStackRotZ;
            mStackScale = mTargetStackScale;
            ApplyVisualTransform();
        }
    }

    private void ResetMotionState()
    {
        mDragOffset = Vector3.zero;
        mDragOffsetVelocity = Vector3.zero;
        mTiltX = 0f;
        mTiltXVelocity = 0f;
        mTiltY = 0f;
        mTiltYVelocity = 0f;
        mStackRotVelocity = 0f;
        mStackScaleVelocity = 0f;
    }

    private void ResetStackVisualImmediate()
    {
        mTargetStackRotZ = 0f;
        mTargetStackScale = 1f;
        mStackRotZ = 0f;
        mStackScale = 1f;
    }

    private void ApplyFlatVisualTransform()
    {
        if (mVisualPivot == null)
        {
            return;
        }

        mVisualPivot.localRotation = Quaternion.identity;
        mVisualPivot.localScale = Vector3.one;
        mVisualPivot.localPosition = Vector3.zero;
    }

    private void ApplyDragTilt()
    {
        mDragLayer.localRotation = Quaternion.Euler(mTiltX, mTiltY, 0f);
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

    private void ApplyPlacedSortingOrder()
    {
        var order = mBaseSortingOrder + 100;
        if (mFaceRenderer != null)
        {
            mFaceRenderer.sortingOrder = order;
        }

        if (mBackRenderer != null)
        {
            mBackRenderer.sortingOrder = order;
        }
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
