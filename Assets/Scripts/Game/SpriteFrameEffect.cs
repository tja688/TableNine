using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteFrameEffect : MonoBehaviour
{
    [SerializeField] private Sprite[] mFrames;
    [SerializeField, Min(1f)] private float mFramesPerSecond = 12f;
    [SerializeField] private bool mPlayOnAwake = true;
    [SerializeField] private bool mLoop;
    [SerializeField] private bool mDestroyOnComplete = true;

    private SpriteRenderer mRenderer;
    private Coroutine mPlayCoroutine;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        mRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (mPlayOnAwake)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        Stop();
        mPlayCoroutine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        if (mPlayCoroutine != null)
        {
            StopCoroutine(mPlayCoroutine);
            mPlayCoroutine = null;
        }

        IsPlaying = false;
    }

    public static SpriteFrameEffect PlayAt(
        SpriteFrameEffect prefab,
        Vector3 worldPosition,
        Transform parent = null,
        bool destroyOnComplete = true)
    {
        if (prefab == null)
        {
            return null;
        }

        var instance = Instantiate(prefab, worldPosition, Quaternion.identity, parent);
        instance.mDestroyOnComplete = destroyOnComplete;
        instance.Play();
        return instance;
    }

    private IEnumerator PlayRoutine()
    {
        if (mRenderer == null || mFrames == null || mFrames.Length == 0)
        {
            yield break;
        }

        IsPlaying = true;
        var frameDuration = 1f / Mathf.Max(1f, mFramesPerSecond);

        do
        {
            for (var i = 0; i < mFrames.Length; i++)
            {
                mRenderer.sprite = mFrames[i];
                yield return new WaitForSeconds(frameDuration);
            }
        }
        while (mLoop);

        IsPlaying = false;
        mPlayCoroutine = null;

        if (mDestroyOnComplete)
        {
            Destroy(gameObject);
        }
    }
}
