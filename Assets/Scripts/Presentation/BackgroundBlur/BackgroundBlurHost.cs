using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(90)]
public sealed class BackgroundBlurHost : MonoBehaviour
{
    private static BackgroundBlurHost sInstance;

    private readonly List<BackgroundBlurSession> mSessions = new List<BackgroundBlurSession>();
    private readonly Dictionary<Transform, int> mFocusRefCounts = new Dictionary<Transform, int>();
    private readonly Dictionary<GameObject, int> mStoredLayers = new Dictionary<GameObject, int>();

    private Material mBlurMaterial;
    private Camera mCamera;
    private Camera mFocusCamera;
    private BackgroundBlurCameraEffect mCameraEffect;
    private int mFocusLayer = -1;
    private int mFocusLayerMask;
    private int mOriginalCullingMask;
    private bool mMaskStored;
    private float mDisplayedIntensity;
    private float mIntensityVelocity;

    public static BackgroundBlurHost Instance
    {
        get
        {
            if (sInstance == null)
            {
                var hostObject = new GameObject(nameof(BackgroundBlurHost));
                DontDestroyOnLoad(hostObject);
                sInstance = hostObject.AddComponent<BackgroundBlurHost>();
                sInstance.Initialize();
            }

            return sInstance;
        }
    }

    public bool IsActive => mSessions.Count > 0;
    public int FocusLayer => mFocusLayer;
    public float DisplayedIntensity => mDisplayedIntensity;
    public bool ShouldRenderBlur => IsActive && mDisplayedIntensity > 0.001f;
    public float BlurSize => 1.1f + mDisplayedIntensity * 1.6f;
    public Material BlurMaterial => mBlurMaterial;

    private void Awake()
    {
        if (sInstance != null && sInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        sInstance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void OnDestroy()
    {
        if (sInstance == this)
        {
            sInstance = null;
        }

        if (mBlurMaterial != null)
        {
            Destroy(mBlurMaterial);
        }
    }

    private void Update()
    {
        var targetIntensity = IsActive ? GetCombinedIntensity() : 0f;
        mDisplayedIntensity = Mathf.SmoothDamp(
            mDisplayedIntensity,
            targetIntensity,
            ref mIntensityVelocity,
            IsActive ? 0.22f : 0.28f);

        if (!IsActive && mDisplayedIntensity < 0.001f)
        {
            mDisplayedIntensity = 0f;
            ApplyBlurCullingMask(false);
            return;
        }

        if (IsActive || mDisplayedIntensity > 0.001f)
        {
            EnsureCameraStack();
            ApplyBlurCullingMask(true);
        }
    }

    private void Initialize()
    {
        if (mBlurMaterial != null)
        {
            return;
        }

        mFocusLayer = LayerMask.NameToLayer(BackgroundBlur.FocusLayerName);
        mFocusLayerMask = mFocusLayer >= 0 ? 1 << mFocusLayer : 0;

        var shader = Shader.Find("TableNine/BackgroundBlur");
        if (shader != null)
        {
            mBlurMaterial = new Material(shader);
        }
    }

    public BackgroundBlurSession PushSession(float intensity)
    {
        var session = new BackgroundBlurSession(this, intensity);
        mSessions.Add(session);
        EnsureCameraStack();
        ApplyBlurCullingMask(true);
        return session;
    }

    public void PopSession(BackgroundBlurSession session)
    {
        if (session == null)
        {
            return;
        }

        session.ClearFocus();
        mSessions.Remove(session);
        if (!IsActive)
        {
            ClearAllFocus();
        }
    }

    internal void RegisterFocus(Transform target)
    {
        if (target == null || mFocusLayer < 0)
        {
            return;
        }

        if (!mFocusRefCounts.TryGetValue(target, out var count))
        {
            count = 0;
        }

        mFocusRefCounts[target] = count + 1;
        if (count > 0)
        {
            return;
        }

        StoreAndApplyFocusLayer(target);
    }

    internal void UnregisterFocus(Transform target)
    {
        if (target == null || !mFocusRefCounts.TryGetValue(target, out var count))
        {
            return;
        }

        count--;
        if (count > 0)
        {
            mFocusRefCounts[target] = count;
            return;
        }

        mFocusRefCounts.Remove(target);
        RestoreStoredLayers(target);
    }

    private void EnsureCameraStack()
    {
        var mainCamera = ResolveCamera();
        if (mainCamera == null || mBlurMaterial == null)
        {
            return;
        }

        if (mCameraEffect == null)
        {
            mCameraEffect = mainCamera.GetComponent<BackgroundBlurCameraEffect>();
            if (mCameraEffect == null)
            {
                mCameraEffect = mainCamera.gameObject.AddComponent<BackgroundBlurCameraEffect>();
            }

            mCameraEffect.Bind(this);
        }

        if (mFocusCamera == null)
        {
            var focusCameraObject = new GameObject("BackgroundBlurFocusCamera");
            focusCameraObject.transform.SetParent(mainCamera.transform, false);
            mFocusCamera = focusCameraObject.AddComponent<Camera>();
            mFocusCamera.enabled = false;
        }

        SyncFocusCamera(mainCamera);
    }

    private void SyncFocusCamera(Camera mainCamera)
    {
        if (mFocusCamera == null)
        {
            return;
        }

        mFocusCamera.CopyFrom(mainCamera);
        mFocusCamera.clearFlags = CameraClearFlags.Depth;
        mFocusCamera.cullingMask = mFocusLayerMask;
        mFocusCamera.depth = mainCamera.depth + 0.1f;
        mFocusCamera.rect = mainCamera.rect;
        mFocusCamera.useOcclusionCulling = false;
    }

    private void ApplyBlurCullingMask(bool active)
    {
        var mainCamera = ResolveCamera();
        if (mainCamera == null)
        {
            return;
        }

        if (active && ShouldRenderBlur)
        {
            if (!mMaskStored)
            {
                mOriginalCullingMask = mainCamera.cullingMask;
                mMaskStored = true;
            }

            mainCamera.cullingMask = mOriginalCullingMask & ~mFocusLayerMask;
            if (mCameraEffect != null)
            {
                mCameraEffect.enabled = true;
            }

            if (mFocusCamera != null)
            {
                SyncFocusCamera(mainCamera);
                mFocusCamera.enabled = mFocusLayerMask != 0;
            }

            return;
        }

        if (mMaskStored)
        {
            mainCamera.cullingMask = mOriginalCullingMask;
            mMaskStored = false;
        }

        if (mCameraEffect != null)
        {
            mCameraEffect.enabled = false;
        }

        if (mFocusCamera != null)
        {
            mFocusCamera.enabled = false;
        }
    }

    private void ClearAllFocus()
    {
        var targets = new List<Transform>(mFocusRefCounts.Keys);
        for (var i = 0; i < targets.Count; i++)
        {
            UnregisterFocus(targets[i]);
        }
    }

    private float GetCombinedIntensity()
    {
        var intensity = 0f;
        for (var i = 0; i < mSessions.Count; i++)
        {
            intensity = Mathf.Max(intensity, mSessions[i].Intensity);
        }

        return intensity;
    }

    private Camera ResolveCamera()
    {
        if (mCamera == null)
        {
            mCamera = Camera.main;
        }

        return mCamera;
    }

    private void StoreAndApplyFocusLayer(Transform root)
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var child = transforms[i];
            if (child == null)
            {
                continue;
            }

            var go = child.gameObject;
            if (!mStoredLayers.ContainsKey(go))
            {
                mStoredLayers[go] = go.layer;
            }

            go.layer = mFocusLayer;
        }
    }

    private void RestoreStoredLayers(Transform root)
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var child = transforms[i];
            if (child == null)
            {
                continue;
            }

            var go = child.gameObject;
            if (mStoredLayers.TryGetValue(go, out var layer))
            {
                go.layer = layer;
                mStoredLayers.Remove(go);
            }
        }
    }
}
