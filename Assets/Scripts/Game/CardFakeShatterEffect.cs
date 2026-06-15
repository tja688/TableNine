using System.Collections;
using UnityEngine;

public sealed class CardFakeShatterSettings
{
    public float ShardScale = 7f;
    public float ScatterStrength = 0.42f;
    public float CrackWidth = 0.038f;
    public float PreBreakAmount = 0.14f;

    public static CardFakeShatterSettings Default => new CardFakeShatterSettings();

    public static CardFakeShatterSettings FromShardCount(int shardCount)
    {
        var scale = Mathf.Clamp(shardCount * 0.42f, 5f, 11f);
        var scatter = Mathf.Clamp(0.34f + shardCount * 0.012f, 0.34f, 0.58f);
        return new CardFakeShatterSettings
        {
            ShardScale = scale,
            ScatterStrength = scatter,
            CrackWidth = 0.034f + shardCount * 0.0008f,
            PreBreakAmount = 0.12f
        };
    }
}

public static class CardFakeShatterEffect
{
    private static readonly int ShatterAmountId = Shader.PropertyToID("_ShatterAmount");
    private static readonly int HitDirectionId = Shader.PropertyToID("_HitDirection");
    private static readonly int ShardScaleId = Shader.PropertyToID("_ShardScale");
    private static readonly int ScatterStrengthId = Shader.PropertyToID("_ScatterStrength");
    private static readonly int CrackWidthId = Shader.PropertyToID("_CrackWidth");
    private static readonly int ImpactUvId = Shader.PropertyToID("_ImpactUv");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private static Shader sShader;

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

        var shader = GetShader();
        if (shader == null)
        {
            yield break;
        }

        settings ??= CardFakeShatterSettings.Default;
        var originalSharedMaterial = faceRenderer.sharedMaterial;
        var shatterMaterial = new Material(shader)
        {
            mainTexture = faceRenderer.sprite.texture
        };
        shatterMaterial.SetColor(ColorId, faceRenderer.color);
        shatterMaterial.SetFloat(ShardScaleId, settings.ShardScale);
        shatterMaterial.SetFloat(ScatterStrengthId, settings.ScatterStrength);
        shatterMaterial.SetFloat(CrackWidthId, settings.CrackWidth);
        shatterMaterial.SetVector(ImpactUvId, WorldToSpriteUv(faceRenderer, worldImpactPoint));

        var localHitDirection = faceRenderer.transform.InverseTransformDirection(worldHitDirection);
        if (localHitDirection.sqrMagnitude <= 0.0001f)
        {
            localHitDirection = Vector3.right;
        }

        localHitDirection.Normalize();
        shatterMaterial.SetVector(HitDirectionId, new Vector4(localHitDirection.x, localHitDirection.y, 0f, 0f));
        shatterMaterial.SetFloat(ShatterAmountId, 0f);

        var backWasEnabled = backRenderer != null && backRenderer.enabled;
        faceRenderer.material = shatterMaterial;
        if (backRenderer != null)
        {
            backRenderer.enabled = false;
        }

        if (impactHold > 0f)
        {
            var elapsed = 0f;
            while (elapsed < impactHold)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / impactHold);
                shatterMaterial.SetFloat(ShatterAmountId, Mathf.Lerp(0f, settings.PreBreakAmount, EaseOutQuad(t)));
                yield return null;
            }
        }

        var duration = Mathf.Max(0.01f, shatterDuration);
        var startAmount = settings.PreBreakAmount;
        var shatterElapsed = 0f;
        while (shatterElapsed < duration)
        {
            shatterElapsed += Time.deltaTime;
            var t = Mathf.Clamp01(shatterElapsed / duration);
            shatterMaterial.SetFloat(ShatterAmountId, Mathf.Lerp(startAmount, 1f, EaseOutCubic(t)));
            yield return null;
        }

        shatterMaterial.SetFloat(ShatterAmountId, 1f);

        if (faceRenderer != null)
        {
            faceRenderer.sharedMaterial = originalSharedMaterial;
            faceRenderer.enabled = false;
        }

        if (backRenderer != null)
        {
            backRenderer.enabled = backWasEnabled;
        }

        Object.Destroy(shatterMaterial);
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

    private static Shader GetShader()
    {
        if (sShader != null)
        {
            return sShader;
        }

        sShader = Shader.Find("TableNine/CardFakeShatter");
        if (sShader == null)
        {
            Debug.LogWarning("CardFakeShatterEffect: shader TableNine/CardFakeShatter not found.");
        }

        return sShader;
    }

    private static Vector4 WorldToSpriteUv(SpriteRenderer renderer, Vector3 worldPoint)
    {
        var localPoint = renderer.transform.InverseTransformPoint(worldPoint);
        var sprite = renderer.sprite;
        var bounds = sprite.bounds;
        var x = Mathf.InverseLerp(bounds.min.x, bounds.max.x, localPoint.x);
        var y = Mathf.InverseLerp(bounds.min.y, bounds.max.y, localPoint.y);
        return new Vector4(Mathf.Clamp01(x), Mathf.Clamp01(y), 0f, 0f);
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
