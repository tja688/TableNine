using System.Collections;
using UnityEngine;

public sealed class CardFakeShatterSettings
{
    public int Rows = 7;
    public int Columns = 5;
    public float Force = 12f;
    public float InnerForce = 8f;
    public float RandomForce = 3.5f;
    public float Lifetime = 30f;
    public float LifetimeRandom = 0f;
    public float Gravity = 1.1f;
    public float AngularVelocity = 520f;
    public float HitDirectionBias = 0.72f;
    public float LateralSpread = 0.55f;
    public float FadeStart = 1f;
    public float NoiseStrength = 0.22f;
    public float GridJitter = 0.38f;
    public float ShardSkipChance = 0.12f;
    public float PositionJitter = 0.045f;
    public float DirectionChaos = 0.48f;
    public float SizeVariation = 0.2f;
    public float SpeedVariation = 0.42f;
    public bool HideOriginalOnShatter = true;
    public bool EnableFlash = true;
    public float FlashPeakAlpha = 0.75f;
    public float FlashMaxDuration = 0.04f;
    public float PostShatterBlockDuration = 0.1f;
    public bool EnableRuntimeVariance = true;
    public float RuntimeVarianceStrength = 1f;

    public static CardFakeShatterSettings Default => new CardFakeShatterSettings();

    public CardFakeShatterSettings Clone()
    {
        return new CardFakeShatterSettings
        {
            Rows = Rows,
            Columns = Columns,
            Force = Force,
            InnerForce = InnerForce,
            RandomForce = RandomForce,
            Lifetime = Lifetime,
            LifetimeRandom = LifetimeRandom,
            Gravity = Gravity,
            AngularVelocity = AngularVelocity,
            HitDirectionBias = HitDirectionBias,
            LateralSpread = LateralSpread,
            FadeStart = FadeStart,
            NoiseStrength = NoiseStrength,
            GridJitter = GridJitter,
            ShardSkipChance = ShardSkipChance,
            PositionJitter = PositionJitter,
            DirectionChaos = DirectionChaos,
            SizeVariation = SizeVariation,
            SpeedVariation = SpeedVariation,
            HideOriginalOnShatter = HideOriginalOnShatter,
            EnableFlash = EnableFlash,
            FlashPeakAlpha = FlashPeakAlpha,
            FlashMaxDuration = FlashMaxDuration,
            PostShatterBlockDuration = PostShatterBlockDuration,
            EnableRuntimeVariance = EnableRuntimeVariance,
            RuntimeVarianceStrength = RuntimeVarianceStrength
        };
    }

    public static void ApplyGridFromShardCount(CardFakeShatterSettings settings, int shardCount)
    {
        if (settings == null)
        {
            return;
        }

        ResolveGrid(Mathf.Max(4, shardCount), out var rows, out var columns);
        settings.Rows = rows;
        settings.Columns = columns;
    }

    public static CardFakeShatterSettings FromShardCount(int shardCount)
    {
        ResolveGrid(Mathf.Max(4, shardCount), out var rows, out var columns);
        var force = Mathf.Clamp(10f + shardCount * 0.35f, 10f, 17f);
        var randomForce = Mathf.Clamp(2.4f + shardCount * 0.08f, 2.4f, 5f);
        var angularVelocity = Mathf.Clamp(360f + shardCount * 22f, 420f, 980f);
        return new CardFakeShatterSettings
        {
            Rows = rows,
            Columns = columns,
            Force = force,
            InnerForce = force * 0.62f,
            RandomForce = randomForce,
            Lifetime = 30f,
            LifetimeRandom = 0f,
            AngularVelocity = angularVelocity,
            Gravity = 1.1f,
            HitDirectionBias = 0.7f,
            LateralSpread = 0.5f + shardCount * 0.006f,
            FadeStart = 1f,
            NoiseStrength = 0.18f + shardCount * 0.003f,
            GridJitter = 0.34f + shardCount * 0.004f,
            ShardSkipChance = 0.1f + shardCount * 0.002f,
            DirectionChaos = 0.42f + shardCount * 0.004f,
            SpeedVariation = 0.38f + shardCount * 0.004f
        };
    }

    public void ApplyRuntimeVariance()
    {
        var strength = Mathf.Max(0f, RuntimeVarianceStrength);
        if (strength <= 0.001f)
        {
            return;
        }

        var rowDelta = Random.Range(-1, 2);
        var columnDelta = Random.Range(-1, 2);
        if (Mathf.Approximately(strength, 1f))
        {
            Rows = Mathf.Clamp(Rows + rowDelta, 3, 12);
            Columns = Mathf.Clamp(Columns + columnDelta, 3, 10);
        }
        else
        {
            Rows = Mathf.Clamp(Mathf.RoundToInt(Rows + rowDelta * strength), 3, 12);
            Columns = Mathf.Clamp(Mathf.RoundToInt(Columns + columnDelta * strength), 3, 10);
        }

        Force *= Mathf.Lerp(1f, Random.Range(0.9f, 1.12f), strength);
        InnerForce *= Mathf.Lerp(1f, Random.Range(0.88f, 1.1f), strength);
        RandomForce *= Mathf.Lerp(1f, Random.Range(0.85f, 1.2f), strength);
        HitDirectionBias = Mathf.Clamp(
            HitDirectionBias + Random.Range(-0.12f, 0.1f) * strength,
            0.45f,
            0.92f);
        LateralSpread = Mathf.Clamp(
            LateralSpread + Random.Range(-0.12f, 0.16f) * strength,
            0.2f,
            0.95f);
        GridJitter = Mathf.Clamp(
            GridJitter + Random.Range(-0.08f, 0.1f) * strength,
            0.1f,
            0.6f);
        ShardSkipChance = Mathf.Clamp(
            ShardSkipChance + Random.Range(-0.04f, 0.06f) * strength,
            0f,
            0.3f);
        DirectionChaos = Mathf.Clamp(
            DirectionChaos + Random.Range(-0.1f, 0.12f) * strength,
            0.1f,
            0.85f);
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
    public static CardFakeShatterSettings ResolveSettings(int battleStyleShardCount = 0)
    {
        var profile = CardFakeShatterTuningProfile.FindInScene();
        if (profile != null)
        {
            return profile.CreateRuntimeSettings(battleStyleShardCount);
        }

        if (battleStyleShardCount > 0)
        {
            var settings = CardFakeShatterSettings.FromShardCount(battleStyleShardCount);
            if (settings.EnableRuntimeVariance)
            {
                settings.ApplyRuntimeVariance();
            }

            return settings;
        }

        var fallback = CardFakeShatterSettings.Default.Clone();
        if (fallback.EnableRuntimeVariance)
        {
            fallback.ApplyRuntimeVariance();
        }

        return fallback;
    }

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

        settings ??= ResolveSettings(0);
        var originalColor = faceRenderer.color;
        var visualRoot = FindCardVisualRoot(faceRenderer);

        if (settings.EnableFlash && impactHold > 0f)
        {
            yield return PlayImpactFlash(
                faceRenderer,
                originalColor,
                Mathf.Min(impactHold, settings.FlashMaxDuration),
                settings.FlashPeakAlpha);
        }

        var burstObject = new GameObject("TableNine Card Shatter Burst");
        var shatter = burstObject.AddComponent<ParticleSpriteShatter2D>();
        shatter.Configure(settings);
        var particleLifetime = shatter.Shatter(faceRenderer, worldImpactPoint, worldHitDirection, backRenderer);
        HideCardVisual(visualRoot, faceRenderer, backRenderer);
        Object.Destroy(burstObject, Mathf.Max(particleLifetime, settings.Lifetime + settings.LifetimeRandom) + 1.5f);

        yield return new WaitForSeconds(settings.PostShatterBlockDuration);
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

    private static Transform FindCardVisualRoot(SpriteRenderer faceRenderer)
    {
        if (faceRenderer == null)
        {
            return null;
        }

        var current = faceRenderer.transform;
        while (current != null)
        {
            if (current.name == "VisualPivot")
            {
                return current;
            }

            current = current.parent;
        }

        return faceRenderer.transform.parent;
    }

    private static void HideCardVisual(Transform visualRoot, SpriteRenderer faceRenderer, SpriteRenderer backRenderer)
    {
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(false);
            return;
        }

        if (faceRenderer != null)
        {
            faceRenderer.enabled = false;
        }

        if (backRenderer != null)
        {
            backRenderer.enabled = false;
        }
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
