using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将 SpriteRenderer 按网格切成 mesh 粒子并爆散，适合 Built-in 2D 卡牌碎裂演出。
/// </summary>
[DisallowMultipleComponent]
public sealed class ParticleSpriteShatter2D : MonoBehaviour
{
    [SerializeField] private int mRows = 7;
    [SerializeField] private int mColumns = 5;
    [SerializeField] private float mForce = 3.5f;
    [SerializeField] private float mInnerForce = 1.8f;
    [SerializeField] private float mRandomForce = 1.2f;
    [SerializeField] private float mLifetime = 1f;
    [SerializeField] private float mLifetimeRandom = 0.2f;
    [SerializeField] private float mGravity = 0.15f;
    [SerializeField] private float mAngularVelocity = 240f;
    [SerializeField] private float mHitDirectionBias = 0.35f;
    [SerializeField] private float mFadeStart = 0.65f;
    [SerializeField] private float mNoiseStrength = 0.15f;
    [SerializeField] private bool mHideOriginalOnShatter = true;

    private ParticleSystem mParticleSystem;
    private ParticleSystemRenderer mParticleRenderer;
    private Material mRuntimeMaterial;
    private Mesh[] mShardMeshes;
    private readonly List<Mesh> mOwnedMeshes = new List<Mesh>(64);

    public int Rows => mRows;
    public int Columns => mColumns;
    public float Lifetime => mLifetime;

    public void Configure(CardFakeShatterSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        mRows = settings.Rows;
        mColumns = settings.Columns;
        mForce = settings.Force;
        mInnerForce = settings.InnerForce;
        mRandomForce = settings.RandomForce;
        mLifetime = settings.Lifetime;
        mLifetimeRandom = settings.LifetimeRandom;
        mGravity = settings.Gravity;
        mAngularVelocity = settings.AngularVelocity;
        mHitDirectionBias = settings.HitDirectionBias;
        mFadeStart = settings.FadeStart;
        mNoiseStrength = settings.NoiseStrength;
        mHideOriginalOnShatter = settings.HideOriginalOnShatter;
    }

    public float Shatter(
        SpriteRenderer sourceRenderer,
        Vector3 worldExplosionPoint,
        Vector3 worldHitDirection,
        SpriteRenderer backRenderer = null)
    {
        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            return 0f;
        }

        EnsureParticleSystem();
        ReleaseOwnedMeshes();

        var sprite = sourceRenderer.sprite;
        var texture = sprite.texture;
        if (texture == null)
        {
            return 0f;
        }

        var shardCount = Mathf.Max(1, mRows * mColumns);
        mShardMeshes = new Mesh[shardCount];
        BuildShardMeshes(sprite, mShardMeshes);

        mParticleRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        mParticleRenderer.sortingOrder = sourceRenderer.sortingOrder;
        EnsureMaterial(sourceRenderer);
        mParticleRenderer.SetMeshes(mShardMeshes, shardCount);

        ConfigureModules(shardCount, sourceRenderer.color);
        EmitShards(sourceRenderer, worldExplosionPoint, worldHitDirection, shardCount);

        if (mHideOriginalOnShatter)
        {
            sourceRenderer.enabled = false;
            if (backRenderer != null)
            {
                backRenderer.enabled = false;
            }
        }

        mParticleSystem.Play(true);
        return mLifetime + mLifetimeRandom;
    }

    public void Cleanup()
    {
        if (mParticleSystem != null)
        {
            mParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ReleaseOwnedMeshes();
        mShardMeshes = null;

        if (mRuntimeMaterial != null)
        {
            Destroy(mRuntimeMaterial);
            mRuntimeMaterial = null;
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void EnsureParticleSystem()
    {
        if (mParticleSystem != null)
        {
            return;
        }

        mParticleSystem = gameObject.GetComponent<ParticleSystem>();
        if (mParticleSystem == null)
        {
            mParticleSystem = gameObject.AddComponent<ParticleSystem>();
        }

        mParticleRenderer = gameObject.GetComponent<ParticleSystemRenderer>();
        if (mParticleRenderer == null)
        {
            mParticleRenderer = gameObject.AddComponent<ParticleSystemRenderer>();
        }

        var main = mParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.maxParticles = Mathf.Max(128, mRows * mColumns);

        var emission = mParticleSystem.emission;
        emission.enabled = false;

        var shape = mParticleSystem.shape;
        shape.enabled = false;

        mParticleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        mParticleRenderer.alignment = ParticleSystemRenderSpace.Local;
    }

    private void ConfigureModules(int shardCount, Color startColor)
    {
        var main = mParticleSystem.main;
        main.gravityModifier = mGravity;
        main.maxParticles = shardCount;
        main.startColor = startColor;

        var velocityLimit = mParticleSystem.limitVelocityOverLifetime;
        velocityLimit.enabled = mGravity > 0.01f;
        velocityLimit.dampen = 0.35f;
        velocityLimit.drag = 0.12f;

        var noise = mParticleSystem.noise;
        noise.enabled = mNoiseStrength > 0.001f;
        noise.strength = mNoiseStrength;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.35f;

        var colorOverLifetime = mParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, Mathf.Clamp01(mFadeStart)),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;
    }

    private void EnsureMaterial(SpriteRenderer sourceRenderer)
    {
        var texture = sourceRenderer.sprite.texture;
        if (mRuntimeMaterial != null && mRuntimeMaterial.mainTexture == texture)
        {
            mRuntimeMaterial.color = sourceRenderer.color;
            mParticleRenderer.material = mRuntimeMaterial;
            return;
        }

        if (mRuntimeMaterial != null)
        {
            Destroy(mRuntimeMaterial);
        }

        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }

        mRuntimeMaterial = new Material(shader)
        {
            mainTexture = texture,
            color = sourceRenderer.color
        };
        mParticleRenderer.material = mRuntimeMaterial;
    }

    private void BuildShardMeshes(Sprite sprite, Mesh[] targetMeshes)
    {
        var rect = sprite.rect;
        var pivot = sprite.pivot;
        var pixelsPerUnit = sprite.pixelsPerUnit;
        var texture = sprite.texture;
        var cellWidth = rect.width / mColumns;
        var cellHeight = rect.height / mRows;
        var halfTexelX = 0.5f / texture.width;
        var halfTexelY = 0.5f / texture.height;
        var halfCellWorldW = cellWidth * 0.5f / pixelsPerUnit;
        var halfCellWorldH = cellHeight * 0.5f / pixelsPerUnit;
        var meshIndex = 0;

        for (var row = 0; row < mRows; row++)
        {
            for (var column = 0; column < mColumns; column++)
            {
                var px = column * cellWidth;
                var py = row * cellHeight;
                var u0 = (rect.x + px) / texture.width + halfTexelX;
                var v0 = (rect.y + py) / texture.height + halfTexelY;
                var u1 = (rect.x + px + cellWidth) / texture.width - halfTexelX;
                var v1 = (rect.y + py + cellHeight) / texture.height - halfTexelY;

                var mesh = CreateQuadMesh(halfCellWorldW, halfCellWorldH, u0, v0, u1, v1);
                targetMeshes[meshIndex] = mesh;
                mOwnedMeshes.Add(mesh);
                meshIndex++;
            }
        }
    }

    private static Mesh CreateQuadMesh(float halfWidth, float halfHeight, float u0, float v0, float u1, float v1)
    {
        var mesh = new Mesh
        {
            vertices = new[]
            {
                new Vector3(-halfWidth, -halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight, 0f),
                new Vector3(halfWidth, halfHeight, 0f),
                new Vector3(-halfWidth, halfHeight, 0f)
            },
            uv = new[]
            {
                new Vector2(u0, v0),
                new Vector2(u1, v0),
                new Vector2(u1, v1),
                new Vector2(u0, v1)
            },
            triangles = new[] { 0, 2, 1, 0, 3, 2 }
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void EmitShards(
        SpriteRenderer sourceRenderer,
        Vector3 worldExplosionPoint,
        Vector3 worldHitDirection,
        int shardCount)
    {
        var sprite = sourceRenderer.sprite;
        var rect = sprite.rect;
        var pivot = sprite.pivot;
        var pixelsPerUnit = sprite.pixelsPerUnit;
        var cellWidth = rect.width / mColumns;
        var cellHeight = rect.height / mRows;
        var sourceTransform = sourceRenderer.transform;
        var explosionLocal = sourceTransform.InverseTransformPoint(worldExplosionPoint);
        var hitDirectionLocal = sourceTransform.InverseTransformDirection(worldHitDirection);
        if (hitDirectionLocal.sqrMagnitude <= 0.0001f)
        {
            hitDirectionLocal = Vector3.right;
        }

        hitDirectionLocal.Normalize();
        var hitDirection2D = new Vector2(hitDirectionLocal.x, hitDirectionLocal.y);
        var maxDistance = 0f;
        var localOffsets = new Vector2[shardCount];
        var meshIndex = 0;

        for (var row = 0; row < mRows; row++)
        {
            for (var column = 0; column < mColumns; column++)
            {
                var centerPx = new Vector2(
                    column * cellWidth + cellWidth * 0.5f,
                    row * cellHeight + cellHeight * 0.5f);
                var localOffsetPx = centerPx - pivot;
                var localOffset = localOffsetPx / pixelsPerUnit;
                if (sourceRenderer.flipX)
                {
                    localOffset.x = -localOffset.x;
                }

                if (sourceRenderer.flipY)
                {
                    localOffset.y = -localOffset.y;
                }

                localOffsets[meshIndex] = localOffset;
                maxDistance = Mathf.Max(maxDistance, localOffset.magnitude);
                meshIndex++;
            }
        }

        transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);

        var emitParams = new ParticleSystem.EmitParams
        {
            applyShapeToPosition = false,
            startSize = 1f
        };

        var baseRotationZ = sourceTransform.eulerAngles.z;
        maxDistance = Mathf.Max(maxDistance, 0.001f);

        for (var i = 0; i < shardCount; i++)
        {
            var localOffset = localOffsets[i];
            var worldPosition = sourceTransform.TransformPoint(new Vector3(localOffset.x, localOffset.y, 0f));
            var radial = (localOffset - new Vector2(explosionLocal.x, explosionLocal.y));
            if (radial.sqrMagnitude <= 0.0001f)
            {
                radial = Random.insideUnitCircle;
            }

            radial = (radial.normalized + Random.insideUnitCircle * 0.35f).normalized;
            var direction = Vector2.Lerp(radial, hitDirection2D, mHitDirectionBias).normalized;
            var distanceFactor = Mathf.InverseLerp(0f, maxDistance, localOffset.magnitude);
            var speed = Mathf.Lerp(mInnerForce, mForce, distanceFactor) + Random.Range(0f, mRandomForce);
            var velocity = direction * speed;
            var lifetime = Mathf.Max(0.05f, mLifetime + Random.Range(-mLifetimeRandom, mLifetimeRandom));

            emitParams.position = worldPosition;
            emitParams.velocity = new Vector3(velocity.x, velocity.y, 0f);
            emitParams.startLifetime = lifetime;
            emitParams.rotation3D = new Vector3(0f, 0f, baseRotationZ + Random.Range(-18f, 18f));
            emitParams.angularVelocity3D = new Vector3(0f, 0f, Random.Range(-mAngularVelocity, mAngularVelocity));
            emitParams.meshIndex = i;
            mParticleSystem.Emit(emitParams, 1);
        }
    }

    private void ReleaseOwnedMeshes()
    {
        for (var i = 0; i < mOwnedMeshes.Count; i++)
        {
            if (mOwnedMeshes[i] != null)
            {
                Destroy(mOwnedMeshes[i]);
            }
        }

        mOwnedMeshes.Clear();
    }
}
