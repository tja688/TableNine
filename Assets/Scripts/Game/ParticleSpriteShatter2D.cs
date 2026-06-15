using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将 SpriteRenderer 按不规则网格切成 mesh 粒子并爆散，适合 Built-in 2D 卡牌碎裂演出。
/// </summary>
[DisallowMultipleComponent]
public sealed class ParticleSpriteShatter2D : MonoBehaviour
{
    private const int MaxMeshesPerParticleRenderer = 4;
    private const float MinimumPhysicalLifetime = CardFakeShatterSettings.MinimumPhysicalLifetime;
    private const int ShardSortingOrderBoost = 120;

    private struct ShardSpec
    {
        public Vector2 LocalCenter;
        public float HalfWorldW;
        public float HalfWorldH;
        public float U0;
        public float V0;
        public float U1;
        public float V1;
    }

    [SerializeField] private int mRows = 7;
    [SerializeField] private int mColumns = 5;
    [SerializeField] private float mForce = 12f;
    [SerializeField] private float mInnerForce = 8f;
    [SerializeField] private float mRandomForce = 3.5f;
    [SerializeField] private float mLifetime = 30f;
    [SerializeField] private float mLifetimeRandom = 0f;
    [SerializeField] private float mGravity = 1.1f;
    [SerializeField] private float mAngularVelocity = 520f;
    [SerializeField] private float mHitDirectionBias = 0.72f;
    [SerializeField] private float mLateralSpread = 0.55f;
    [SerializeField] private float mFadeStart = 1f;
    [SerializeField] private float mNoiseStrength = 0f;
    [SerializeField] private float mGridJitter = 0.38f;
    [SerializeField] private float mShardSkipChance = 0.12f;
    [SerializeField] private float mPositionJitter = 0.045f;
    [SerializeField] private float mDirectionChaos = 0.48f;
    [SerializeField] private float mSizeVariation = 0.2f;
    [SerializeField] private float mSpeedVariation = 0.42f;
    [SerializeField] private bool mHideOriginalOnShatter = true;

    private ParticleSystem mParticleSystem;
    private ParticleSystemRenderer mParticleRenderer;
    private Material mRuntimeMaterial;
    private Mesh[] mShardMeshes;
    private readonly List<ParticleSystem> mParticleSystems = new List<ParticleSystem>(12);
    private readonly List<ParticleSystemRenderer> mParticleRenderers = new List<ParticleSystemRenderer>(12);
    private readonly List<Mesh> mOwnedMeshes = new List<Mesh>(64);
    private readonly List<ShardSpec> mShardSpecs = new List<ShardSpec>(64);
    private float[] mRowSplits = new float[16];
    private float[] mColumnSplits = new float[16];

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
        mLateralSpread = settings.LateralSpread;
        mFadeStart = settings.FadeStart;
        mNoiseStrength = settings.NoiseStrength;
        mGridJitter = settings.GridJitter;
        mShardSkipChance = settings.ShardSkipChance;
        mPositionJitter = settings.PositionJitter;
        mDirectionChaos = settings.DirectionChaos;
        mSizeVariation = settings.SizeVariation;
        mSpeedVariation = settings.SpeedVariation;
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
        CleanupForReuse();
        mShardSpecs.Clear();

        var sprite = sourceRenderer.sprite;
        var texture = sprite.texture;
        if (texture == null)
        {
            return 0f;
        }

        BuildRandomShards(sprite, sourceRenderer.flipX, sourceRenderer.flipY);
        if (mShardSpecs.Count == 0)
        {
            return 0f;
        }

        var shardCount = mShardSpecs.Count;
        mShardMeshes = new Mesh[shardCount];
        for (var i = 0; i < shardCount; i++)
        {
            var spec = mShardSpecs[i];
            mShardMeshes[i] = CreateQuadMesh(spec.HalfWorldW, spec.HalfWorldH, spec.U0, spec.V0, spec.U1, spec.V1);
            mOwnedMeshes.Add(mShardMeshes[i]);
        }

        EnsureMaterial(sourceRenderer);

        var batchCount = Mathf.CeilToInt(shardCount / (float)MaxMeshesPerParticleRenderer);
        EnsureParticleBatchCapacity(batchCount);
        ConfigureParticleBatches(shardCount, sourceRenderer);
        EmitShards(sourceRenderer, worldHitDirection, shardCount);
        ApplyLifetimeToAllParticleSystems(GetBaseParticleLifetime());
        LogParticleDiagnostics(shardCount);
        StartSurvivalDiagnostics();

        if (mHideOriginalOnShatter)
        {
            sourceRenderer.enabled = false;
            if (backRenderer != null)
            {
                backRenderer.enabled = false;
            }
        }

        return GetMaxParticleLifetime();
    }

    public void Cleanup()
    {
        ReleaseOwnedMeshes();
        mShardMeshes = null;
        mShardSpecs.Clear();

        if (mRuntimeMaterial != null)
        {
            DestroyOwnedObject(mRuntimeMaterial);
            mRuntimeMaterial = null;
        }
    }

    private void CleanupForReuse()
    {
        if (mParticleSystem != null)
        {
            StopAndClearParticleSystems();
        }

        Cleanup();
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

        mParticleSystems.Clear();
        mParticleRenderers.Clear();
        mParticleSystems.Add(mParticleSystem);
        mParticleRenderers.Add(mParticleRenderer);
        ConfigureParticleSystemBasics(mParticleSystem, mParticleRenderer);
    }

    private void EnsureParticleBatchCapacity(int batchCount)
    {
        EnsureParticleSystem();

        for (var i = mParticleSystems.Count; i < batchCount; i++)
        {
            var batchObject = new GameObject($"Particle Batch {i + 1}");
            batchObject.transform.SetParent(transform, false);
            batchObject.transform.localPosition = Vector3.zero;
            batchObject.transform.localRotation = Quaternion.identity;
            batchObject.transform.localScale = Vector3.one;

            var particleSystem = batchObject.AddComponent<ParticleSystem>();
            var particleRenderer = batchObject.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer == null)
            {
                particleRenderer = batchObject.AddComponent<ParticleSystemRenderer>();
            }

            mParticleSystems.Add(particleSystem);
            mParticleRenderers.Add(particleRenderer);
            ConfigureParticleSystemBasics(particleSystem, particleRenderer);
        }
    }

    private void ConfigureParticleSystemBasics(ParticleSystem particleSystem, ParticleSystemRenderer particleRenderer)
    {
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.maxParticles = 160;

        var emission = particleSystem.emission;
        emission.enabled = false;

        var shape = particleSystem.shape;
        shape.enabled = false;

        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.alignment = ParticleSystemRenderSpace.Local;
        particleRenderer.minParticleSize = 0f;
        particleRenderer.maxParticleSize = 1000f;
    }

    private void ConfigureParticleBatches(int shardCount, SpriteRenderer sourceRenderer)
    {
        var batchCount = Mathf.CeilToInt(shardCount / (float)MaxMeshesPerParticleRenderer);
        for (var batchIndex = 0; batchIndex < mParticleSystems.Count; batchIndex++)
        {
            var particleSystem = mParticleSystems[batchIndex];
            var particleRenderer = mParticleRenderers[batchIndex];
            var activeBatch = batchIndex < batchCount;
            particleRenderer.enabled = activeBatch;

            if (!activeBatch)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                continue;
            }

            var meshStart = batchIndex * MaxMeshesPerParticleRenderer;
            var meshCount = Mathf.Min(MaxMeshesPerParticleRenderer, shardCount - meshStart);
            var batchMeshes = new Mesh[meshCount];
            for (var i = 0; i < meshCount; i++)
            {
                batchMeshes[i] = mShardMeshes[meshStart + i];
            }

            particleRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            particleRenderer.sortingOrder = sourceRenderer.sortingOrder + ShardSortingOrderBoost;
            particleRenderer.material = mRuntimeMaterial;
            particleRenderer.SetMeshes(batchMeshes, meshCount);

            ConfigureModules(particleSystem, meshCount, sourceRenderer.color);
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void ApplyLifetimeToAllParticleSystems(float lifetime)
    {
        var lifetimeCurve = new ParticleSystem.MinMaxCurve(lifetime);
        var particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        for (var i = 0; i < particleSystems.Length; i++)
        {
            var particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            var main = particleSystem.main;
            main.startLifetime = lifetimeCurve;
            DisableVisualFadeModules(particleSystem);
        }
    }

    private static void DisableVisualFadeModules(ParticleSystem particleSystem)
    {
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = false;

        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = false;

        var rotationOverLifetime = particleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = false;

        var sizeBySpeed = particleSystem.sizeBySpeed;
        sizeBySpeed.enabled = false;

        var colorBySpeed = particleSystem.colorBySpeed;
        colorBySpeed.enabled = false;

        var rotationBySpeed = particleSystem.rotationBySpeed;
        rotationBySpeed.enabled = false;

        var textureSheetAnimation = particleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = false;

        var trails = particleSystem.trails;
        trails.enabled = false;

        var lights = particleSystem.lights;
        lights.enabled = false;

        var customData = particleSystem.customData;
        customData.enabled = false;

        var noise = particleSystem.noise;
        noise.enabled = false;

        var collision = particleSystem.collision;
        collision.enabled = false;

        var trigger = particleSystem.trigger;
        trigger.enabled = false;

        var externalForces = particleSystem.externalForces;
        externalForces.enabled = false;
    }

    private void ConfigureModules(ParticleSystem particleSystem, int particlesInBatch, Color startColor)
    {
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particleSystem.main;
        main.gravityModifier = mGravity;
        main.maxParticles = Mathf.Max(particlesInBatch + 8, 32);
        main.startColor = startColor;
        main.startLifetime = GetBaseParticleLifetime();
        main.startSpeed = 0f;
        main.simulationSpeed = 1f;
        main.duration = Mathf.Max(GetMaxParticleLifetime() + 2f, MinimumPhysicalLifetime + 2f);
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = particleSystem.emission;
        emission.enabled = false;

        var velocityLimit = particleSystem.limitVelocityOverLifetime;
        velocityLimit.enabled = false;

        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = false;

        var forceOverLifetime = particleSystem.forceOverLifetime;
        forceOverLifetime.enabled = false;

        DisableVisualFadeModules(particleSystem);
    }

    private void EnsureMaterial(SpriteRenderer sourceRenderer)
    {
        var texture = sourceRenderer.sprite.texture;
        if (mRuntimeMaterial != null && mRuntimeMaterial.mainTexture == texture)
        {
            mRuntimeMaterial.color = sourceRenderer.color;
            ApplyRuntimeMaterialToAllRenderers();
            return;
        }

        if (mRuntimeMaterial != null)
        {
            DestroyOwnedObject(mRuntimeMaterial);
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

        ApplyRuntimeMaterialToAllRenderers();
    }

    private void ApplyRuntimeMaterialToAllRenderers()
    {
        if (mRuntimeMaterial == null)
        {
            return;
        }

        var renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material = mRuntimeMaterial;
            }
        }
    }

    private void BuildRandomShards(Sprite sprite, bool flipX, bool flipY)
    {
        var rect = sprite.rect;
        var pivot = sprite.pivot;
        var pixelsPerUnit = sprite.pixelsPerUnit;
        var texture = sprite.texture;
        var halfTexelX = 0.5f / texture.width;
        var halfTexelY = 0.5f / texture.height;
        var baseCellWidth = rect.width / mColumns;
        var baseCellHeight = rect.height / mRows;

        BuildJitteredSplits(rect.width, mColumns, baseCellWidth, mGridJitter, mColumnSplits);
        BuildJitteredSplits(rect.height, mRows, baseCellHeight, mGridJitter, mRowSplits);

        for (var row = 0; row < mRows; row++)
        {
            for (var column = 0; column < mColumns; column++)
            {
                if (Random.value < mShardSkipChance)
                {
                    continue;
                }

                var px0 = mColumnSplits[column];
                var px1 = mColumnSplits[column + 1];
                var py0 = mRowSplits[row];
                var py1 = mRowSplits[row + 1];
                if (px1 - px0 < 1.5f || py1 - py0 < 1.5f)
                {
                    continue;
                }

                var centerPx = new Vector2(
                    (px0 + px1) * 0.5f + Random.Range(-baseCellWidth * 0.08f, baseCellWidth * 0.08f),
                    (py0 + py1) * 0.5f + Random.Range(-baseCellHeight * 0.08f, baseCellHeight * 0.08f));
                var localOffsetPx = centerPx - pivot;
                var localCenter = localOffsetPx / pixelsPerUnit;
                if (flipX)
                {
                    localCenter.x = -localCenter.x;
                }

                if (flipY)
                {
                    localCenter.y = -localCenter.y;
                }

                var halfWorldW = ((px1 - px0) * 0.5f / pixelsPerUnit) * Random.Range(1f - mSizeVariation, 1f + mSizeVariation);
                var halfWorldH = ((py1 - py0) * 0.5f / pixelsPerUnit) * Random.Range(1f - mSizeVariation, 1f + mSizeVariation);
                var u0 = (rect.x + px0) / texture.width + halfTexelX;
                var v0 = (rect.y + py0) / texture.height + halfTexelY;
                var u1 = (rect.x + px1) / texture.width - halfTexelX;
                var v1 = (rect.y + py1) / texture.height - halfTexelY;

                mShardSpecs.Add(new ShardSpec
                {
                    LocalCenter = localCenter,
                    HalfWorldW = halfWorldW,
                    HalfWorldH = halfWorldH,
                    U0 = u0,
                    V0 = v0,
                    U1 = u1,
                    V1 = v1
                });
            }
        }
    }

    private static void BuildJitteredSplits(float totalSize, int count, float baseCellSize, float jitter, float[] splits)
    {
        splits[0] = 0f;
        splits[count] = totalSize;

        for (var i = 1; i < count; i++)
        {
            var ideal = i * (totalSize / count);
            var offset = Random.Range(-baseCellSize, baseCellSize) * jitter;
            splits[i] = Mathf.Clamp(ideal + offset, splits[i - 1] + baseCellSize * 0.28f, totalSize - (count - i) * baseCellSize * 0.28f);
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

    private void EmitShards(SpriteRenderer sourceRenderer, Vector3 worldHitDirection, int shardCount)
    {
        var sourceTransform = sourceRenderer.transform;
        var hitDirection = new Vector2(worldHitDirection.x, worldHitDirection.y);
        if (hitDirection.sqrMagnitude <= 0.0001f)
        {
            hitDirection = Vector2.right;
        }

        hitDirection.Normalize();
        var perpendicular = new Vector2(-hitDirection.y, hitDirection.x);
        var cardCenter = sourceTransform.position;
        var maxLateral = 0.001f;

        for (var i = 0; i < shardCount; i++)
        {
            var localOffset = mShardSpecs[i].LocalCenter;
            var worldOffset = sourceTransform.TransformVector(new Vector3(localOffset.x, localOffset.y, 0f));
            maxLateral = Mathf.Max(maxLateral, Mathf.Abs(Vector2.Dot(new Vector2(worldOffset.x, worldOffset.y), perpendicular)));
        }

        transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);

        var emitParams = new ParticleSystem.EmitParams
        {
            applyShapeToPosition = false
        };

        var baseRotationZ = sourceTransform.eulerAngles.z;
        maxLateral = Mathf.Max(maxLateral, 0.001f);

        for (var i = 0; i < shardCount; i++)
        {
            var spec = mShardSpecs[i];
            var localOffset = spec.LocalCenter;
            var worldPosition = sourceTransform.TransformPoint(new Vector3(localOffset.x, localOffset.y, 0f));
            worldPosition += new Vector3(
                Random.Range(-mPositionJitter, mPositionJitter),
                Random.Range(-mPositionJitter, mPositionJitter),
                0f);

            var worldOffset = new Vector2(worldPosition.x - cardCenter.x, worldPosition.y - cardCenter.y);
            var lateral = Vector2.Dot(worldOffset, perpendicular) / maxLateral;
            var shardHitBias = Mathf.Clamp(mHitDirectionBias + Random.Range(-0.18f, 0.14f), 0.45f, 0.92f);
            var shardLateral = mLateralSpread * Random.Range(0.55f, 1.35f);
            var direction = hitDirection * shardHitBias;
            direction += perpendicular * lateral * shardLateral;
            direction += Random.insideUnitCircle * mDirectionChaos;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = hitDirection;
            }

            direction.Normalize();

            var worldOffsetMagnitude = worldOffset.magnitude;
            var forwardAlignment = worldOffsetMagnitude > 0.001f
                ? Vector2.Dot(worldOffset / worldOffsetMagnitude, hitDirection)
                : 0f;
            var forwardFactor = Random.Range(0.65f, 1.15f) + Mathf.Clamp01(forwardAlignment) * Random.Range(0.15f, 0.55f);
            var baseSpeed = Mathf.Lerp(mInnerForce, mForce, Mathf.Clamp01(forwardAlignment + 0.35f));
            var speed = (baseSpeed + Random.Range(0f, mRandomForce)) * forwardFactor;
            speed *= Random.Range(1f - mSpeedVariation, 1f + mSpeedVariation);
            var velocity = direction * speed;
            var lifetime = GetParticleLifetime();

            emitParams.position = worldPosition;
            emitParams.velocity = new Vector3(velocity.x, velocity.y, 0f);
            emitParams.startLifetime = lifetime;
            emitParams.startSize3D = new Vector3(
                Random.Range(1f - mSizeVariation * 0.65f, 1f + mSizeVariation * 0.45f),
                Random.Range(1f - mSizeVariation * 0.65f, 1f + mSizeVariation * 0.45f),
                1f);
            emitParams.rotation3D = new Vector3(0f, 0f, baseRotationZ + Random.Range(-42f, 42f));
            emitParams.angularVelocity3D = new Vector3(
                0f,
                0f,
                Random.Range(-mAngularVelocity, mAngularVelocity) * Random.Range(0.55f, 1.35f));
            emitParams.meshIndex = i % MaxMeshesPerParticleRenderer;
            mParticleSystems[i / MaxMeshesPerParticleRenderer].Emit(emitParams, 1);
        }
    }

    private int GetTotalAliveParticleCount()
    {
        var total = 0;
        var particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        for (var i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                total += particleSystems[i].particleCount;
            }
        }

        return total;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogParticleDiagnostics(int shardCount)
    {
        var particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        for (var i = 0; i < particleSystems.Length; i++)
        {
            var particleSystem = particleSystems[i];
            if (particleSystem == null || !particleSystem.gameObject.activeInHierarchy)
            {
                continue;
            }

            var main = particleSystem.main;
            Debug.Log(
                $"[Shatter] {particleSystem.name} emitLifetime={GetBaseParticleLifetime():F2} " +
                $"main.startLifetime={main.startLifetime.constant:F2} maxParticles={main.maxParticles} " +
                $"colorFade={particleSystem.colorOverLifetime.enabled} sizeFade={particleSystem.sizeOverLifetime.enabled} " +
                $"alive={particleSystem.particleCount}",
                particleSystem);
        }

        Debug.Log($"[Shatter] batches={particleSystems.Length} shards={shardCount} totalAlive={GetTotalAliveParticleCount()}");
    }

    private void StartSurvivalDiagnostics()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        StartCoroutine(SurvivalDiagnosticsRoutine());
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private IEnumerator SurvivalDiagnosticsRoutine()
    {
        var elapsed = 0f;
        var nextCheckpoint = 1f;
        while (elapsed < GetMaxParticleLifetime() + 0.5f)
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed + 0.001f < nextCheckpoint)
            {
                continue;
            }

            Debug.Log($"[Shatter] t={nextCheckpoint:F0}s totalAlive={GetTotalAliveParticleCount()}");
            nextCheckpoint = nextCheckpoint < 3f ? 3f
                : nextCheckpoint < 5f ? 5f
                : nextCheckpoint < 10f ? 10f
                : nextCheckpoint + 10f;
        }
    }
#endif

    private float GetBaseParticleLifetime()
    {
        return Mathf.Max(MinimumPhysicalLifetime, mLifetime);
    }

    private float GetMaxParticleLifetime()
    {
        return GetBaseParticleLifetime() + Mathf.Max(0f, mLifetimeRandom);
    }

    private float GetParticleLifetime()
    {
        var baseLifetime = GetBaseParticleLifetime();
        if (mLifetimeRandom <= 0.001f)
        {
            return baseLifetime;
        }

        return Mathf.Max(MinimumPhysicalLifetime, Random.Range(baseLifetime, baseLifetime + mLifetimeRandom));
    }

    private void StopAndClearParticleSystems()
    {
        for (var i = 0; i < mParticleSystems.Count; i++)
        {
            if (mParticleSystems[i] != null)
            {
                mParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                mParticleSystems[i].Clear(true);
            }
        }
    }

    private void ReleaseOwnedMeshes()
    {
        for (var i = 0; i < mOwnedMeshes.Count; i++)
        {
            if (mOwnedMeshes[i] != null)
            {
                DestroyOwnedObject(mOwnedMeshes[i]);
            }
        }

        mOwnedMeshes.Clear();
    }

    private static void DestroyOwnedObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
