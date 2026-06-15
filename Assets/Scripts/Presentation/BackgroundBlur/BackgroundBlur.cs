using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 世界空间背景模糊调度入口。Push/Pop 引用计数；Focus 图层目标保持清晰；UI 不参与模糊。
/// </summary>
public static class BackgroundBlur
{
    public const string FocusLayerName = "Focus";

    public static bool IsActive => BackgroundBlurHost.Instance != null && BackgroundBlurHost.Instance.IsActive;

    public static BackgroundBlurSession Push(float intensity = 1f)
    {
        return BackgroundBlurHost.Instance.PushSession(intensity);
    }

    public static void Pop(BackgroundBlurSession session)
    {
        if (session == null)
        {
            return;
        }

        BackgroundBlurHost.Instance.PopSession(session);
    }

    public static int FocusLayer => BackgroundBlurHost.Instance.FocusLayer;
}

public sealed class BackgroundBlurSession : IDisposable
{
    private readonly BackgroundBlurHost mHost;
    private readonly HashSet<Transform> mFocused = new HashSet<Transform>();
    private bool mDisposed;

    internal BackgroundBlurSession(BackgroundBlurHost host, float intensity)
    {
        mHost = host;
        Intensity = Mathf.Clamp01(intensity);
    }

    public float Intensity { get; private set; }

    public void SetIntensity(float intensity)
    {
        Intensity = Mathf.Clamp01(intensity);
    }

    public void Focus(Transform target)
    {
        if (target == null || mDisposed)
        {
            return;
        }

        if (mFocused.Add(target))
        {
            mHost.RegisterFocus(target);
        }
    }

    public void Unfocus(Transform target)
    {
        if (target == null || mDisposed)
        {
            return;
        }

        if (mFocused.Remove(target))
        {
            mHost.UnregisterFocus(target);
        }
    }

    public void ClearFocus()
    {
        foreach (var target in mFocused)
        {
            if (target != null)
            {
                mHost.UnregisterFocus(target);
            }
        }

        mFocused.Clear();
    }

    public void Dispose()
    {
        if (mDisposed)
        {
            return;
        }

        mDisposed = true;
        ClearFocus();
        BackgroundBlur.Pop(this);
    }
}
