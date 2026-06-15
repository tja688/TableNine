using System.Collections;
using UnityEngine;

public sealed class CardFakeShatterSettings
{
    public int Rows = 7;
    public int Columns = 5;
    public float Force = 3.5f;
    public float InnerForce = 1.8f;
    public float RandomForce = 1.2f;
    public float Lifetime = 1f;
    public float LifetimeRandom = 0.2f;
    public float Gravity = 0.15f;
    public float AngularVelocity = 240f;
    public float HitDirectionBias = 0.35f;
    public float FadeStart = 0.65f;
    public float NoiseStrength = 0.15f;
    public bool HideOriginalOnShatter = true;
    public bool EnableFlash = true;
    public float FlashPeakAlpha = 0.75f;

    public static CardFakeShatterSettings Default => new CardFakeShatterSettings();

    public static CardFakeShatterSettings FromShardCount(int shardCount)
    {
        ResolveGrid(Mathf.Max(4, shardCount), out var rows, out var columns);
        var force = Mathf.Clamp(2.4f + shardCount * 0.05f, 2.8f, 5.5f);
        var randomForce = Mathf.Clamp(0.8f + shardCount * 0.02f, 0.8f, 1.8f);
        var angularVelocity = Mathf.Clamp(180f + shardCount * 12f, 220f, 760f);
        return new CardFakeShatterSettings
        {
            Rows = rows,
            Columns = columns,
            Force = force,
            InnerForce = force * 0.5f,
            RandomForce = randomForce,
            Lifetime = Mathf.Clamp(0.75f + shardCount * 0.012f, 0.75f, 1.35f),
            AngularVelocity = angularVelocity,
            Gravity = shardCount >= 18 ? 0.08f : 0.15f,
            NoiseStrength = 0.12f + shardCount * 0.002f
        };
    }

    private static void ResolveGrid(int targetCount, out int rows, out int columns)
    {
        const float cardAspect = 1.35f;
        columns = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(targetCount / cardAspect)), 3, 10);
        rows = Mathf.Clamp(Mathf.CeilToInt(targetCount / (float)columns), 3, 12);
    }
}

public static class CardFakeShatterEffect
{
    private static ParticleSpriteShatter2D sSharedShatter;

    public static IEnumerator PlayOnCardView(
        CardView cardView,
        Vector3 worldImpactPoint,
        Vector3 worldHitDirection,
        float impactHold,
        float shatterDuration,
        CardFakeShatterSettings settings = null)
    {
        if (!TryGetFaceRenderer(cardView, out var faceRenderer, out var backRenderer))
        {
            yield break;
        }

        yield return Play(faceRenderer, backRenderer, worldImpactPoint, worldHitDirection, impactHold, shatterDuration, settings);
    }

    public static IEnumerator PlayOnTransform(
        Transform cardRoot,
        Vector3 worldImpactPoint,
        Vector3 worldHitDirection,
        float impactHold,
        float shatterDuration,
        CardFakeShatterSettings settings = null)
    {
        if (cardRoot == null)
        {
            yield break;
        }

        var cardView = cardRoot.GetComponentInChildren<CardView>(true);
        if (cardView != null)
        {
            yield return PlayOnCardView(cardView, worldImpactPoint, worldHitDirection, impactHold, shatterDuration, settings);
            yield break;
        }

        var faceRenderer = cardRoot.GetComponentInChildren<SpriteRenderer>(true);
        yield return Play(faceRenderer, null, worldImpactPoint, worldHitDirection, impactHold, shatterDuration, settings);
    }

    public static IEnumerator Play(
        SpriteRenderer faceRenderer,
        SpriteRenderer backRenderer,
        Vector3 worldImpactPoint,
        Vector3 worldHitDirection,
        float impactHold,
        float shatterDuration,
        CardFakeShatterSettings settings = null)
    {
        if (faceRenderer == null || faceRenderer.sprite == null)
        {
            yield break;
        }

        settings ??= CardFakeShatterSettings.Default;
        var originalColor = faceRenderer.color;
        var backWasEnabled = backRenderer != null && backRenderer.enabled;

        if (settings.EnableFlash && impactHold > 0f)
        {
            yield return PlayImpactFlash(faceRenderer, originalColor, impactHold, settings.FlashPeakAlpha);
        }
        else if (impactHold > 0f)
        {
            yield return new WaitForSeconds(impactHold);
        }

        var shatter = GetOrCreateSharedShatter();
        shatter.Configure(settings);
        var particleLifetime = shatter.Shatter(faceRenderer, worldImpactPoint, worldHitDirection, backRenderer);
        var waitDuration = Mathf.Max(shatterDuration, particleLifetime);

        yield return new WaitForSeconds(waitDuration);

        faceRenderer.color = originalColor;
        faceRenderer.enabled = false;
        if (backRenderer != null)
        {
            backRenderer.enabled = backWasEnabled;
        }
    }

    public static Vector3 ComputeImpactPoint(SpriteRenderer faceRenderer, Vector3 targetCenter, Vector3 hitDirection)
    {
        if (faceRenderer == null)
        {
            return targetCenter;
        }

        var direction = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector3.right;
        var extent = Mathf.Max(faceRenderer.bounds.extents.x, faceRenderer.bounds.extents.y) * 0.42f;
        return targetCenter - direction * extent;
    }

    public static bool TryGetFaceRenderer(CardView cardView, out SpriteRenderer faceRenderer, out SpriteRenderer backRenderer)
    {
        faceRenderer = null;
        backRenderer = null;
        if (cardView?.DisplayAdapter?.VisualPivot == null)
        {
            return false;
        }

        var pivot = cardView.DisplayAdapter.VisualPivot;
        var faceTransform = pivot.Find("Face");
        var backTransform = pivot.Find("Back");
        faceRenderer = faceTransform != null ? faceTransform.GetComponent<SpriteRenderer>() : null;
        backRenderer = backTransform != null ? backTransform.GetComponent<SpriteRenderer>() : null;
        return faceRenderer != null && faceRenderer.sprite != null;
    }

    private static ParticleSpriteShatter2D GetOrCreateSharedShatter()
    {
        if (sSharedShatter != null)
        {
            sSharedShatter.Cleanup();
            return sSharedShatter;
        }

        var shatterObject = new GameObject("TableNine Shared Particle Shatter");
        Object.DontDestroyOnLoad(shatterObject);
        sSharedShatter = shatterObject.AddComponent<ParticleSpriteShatter2D>();
        return sSharedShatter;
    }

    private static IEnumerator PlayImpactFlash(
        SpriteRenderer faceRenderer,
        Color originalColor,
        float duration,
        float peakAlpha)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var flash = 1f - EaseOutQuad(t);
            var color = Color.Lerp(originalColor, Color.white, flash * peakAlpha);
            faceRenderer.color = color;
            yield return null;
        }

        faceRenderer.color = originalColor;
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
