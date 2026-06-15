using System;
using UnityEngine;

/// <summary>
/// 卡牌粒子碎裂的 Inspector 可调参数（Serializable，可内嵌组件或 ScriptableObject）。
/// </summary>
[Serializable]
public sealed class CardFakeShatterTuning
{
    [Header("碎裂网格")]
    [Tooltip("竖向切几行。越大碎片越小、数量越多。与 Columns 相乘 ≈ 最大碎片数。\n建议：小卡 5–7，普通 7–9，特写 9–12。")]
    [Range(3, 14)]
    public int Rows = 7;

    [Tooltip("横向切几列。卡牌偏高时可略小于 Rows。\n与 Rows 相乘为理论碎片上限（仍会受跳过概率影响）。")]
    [Range(3, 12)]
    public int Columns = 5;

    [Header("飞行速度")]
    [Tooltip("碎片飞散的基础最大速度（世界单位/秒）。整体「甩出去」的力度主要看这个。\n值得：12–16，太猛：>19。")]
    [Range(4f, 24f)]
    public float Force = 12f;

    [Tooltip("靠撞击反侧 / 卡牌内侧碎片的最低速度。与 Force 拉开差距可做出「外侧快、内侧慢」。\n通常设为 Force 的 0.55–0.75。")]
    [Range(2f, 18f)]
    public float InnerForce = 8f;

    [Tooltip("每片额外随机速度加成（0 ~ 该值）。越大同一次碎裂里快慢差异越明显。")]
    [Range(0f, 8f)]
    public float RandomForce = 3.5f;

    [Tooltip("每片速度的随机倍率幅度（±该比例）。0.42 表示约 0.58x ~ 1.42x。\n想要更乱可加到 0.55。")]
    [Range(0f, 0.8f)]
    public float SpeedVariation = 0.42f;

    [Header("方向")]
    [Tooltip("沿撞击方向飞行的权重（0=全随机，1=完全沿撞击方向）。\n想要「被打飞」：0.7–0.85；想要炸开：0.45–0.6。")]
    [Range(0f, 1f)]
    public float HitDirectionBias = 0.72f;

    [Tooltip("横向扩散强度。碎片沿撞击方向法线散开的程度。\n越大越「扇形铺开」，越小越像一条直线飞出去。")]
    [Range(0f, 1.2f)]
    public float LateralSpread = 0.55f;

    [Tooltip("方向混沌度。每片叠加随机方向噪声，越大越不按套路飞。\n固定感强时提高到 0.5–0.65。")]
    [Range(0f, 1f)]
    public float DirectionChaos = 0.48f;

    [Header("重力与旋转")]
    [Tooltip("粒子重力倍率（× Physics.gravity）。1.0=正常下落；0=飘飞；1.5=明显坠落。\n觉得太慢请加到 1.2–1.6。")]
    [Range(0f, 2.5f)]
    public float Gravity = 1.1f;

    [Tooltip("碎片 Z 轴角速度随机范围（度/秒）。越大转得越疯。")]
    [Range(0f, 1200f)]
    public float AngularVelocity = 520f;

    [Header("持续时间")]
    [Tooltip("碎片物理存活时间（秒）。默认 30s，全程不渐隐；代码层会兜底保证至少 10s 内不因生命周期回收。")]
    [Range(10f, 60f)]
    public float Lifetime = 30f;

    [Tooltip("存活时间随机波动（±秒）。0 = 每片统一寿命。")]
    [Range(0f, 1.5f)]
    public float LifetimeRandom = 0f;

    [Tooltip("生命周期渐变中「开始淡出」的时间点（0–1）。1 = 不渐隐，到期直接消失。")]
    [Range(0.4f, 1f)]
    public float FadeStart = 1f;

    [Header("碎裂随机感")]
    [Tooltip("切分线抖动。0=整齐网格；0.35–0.45=裂纹参差；>0.5 可能重叠或出界。")]
    [Range(0f, 0.65f)]
    public float GridJitter = 0.38f;

    [Tooltip("每个格子被跳过的概率。留空越多破洞感越强；0=每格必出。")]
    [Range(0f, 0.35f)]
    public float ShardSkipChance = 0.12f;

    [Tooltip("发射位置抖动（世界单位）。让碎片不是从完美格心弹出。")]
    [Range(0f, 0.15f)]
    public float PositionJitter = 0.045f;

    [Tooltip("单片尺寸随机幅度（±比例）。配合 GridJitter 让碎块大小不一。")]
    [Range(0f, 0.45f)]
    public float SizeVariation = 0.2f;

    [Header("粒子扰动")]
    [Tooltip("噪声模块强度。飞行中的轻微飘动；过高会像火焰抖动。")]
    [Range(0f, 0.6f)]
    public float NoiseStrength = 0.22f;

    [Header("撞击闪白")]
    [Tooltip("碎裂前是否闪白。关闭则撞击后立刻碎裂。")]
    public bool EnableFlash = true;

    [Tooltip("闪白叠加强度（0–1）。1=完全变白。")]
    [Range(0f, 1f)]
    public float FlashPeakAlpha = 0.75f;

    [Tooltip("闪白最长持续时间（秒）。实际还会被战斗风格的 ImpactHold 截断。")]
    [Range(0f, 0.2f)]
    public float FlashMaxDuration = 0.04f;

    [Header("卡面隐藏")]
    [Tooltip("碎裂后隐藏原 Sprite（VisualPivot）。一般保持开启。")]
    public bool HideOriginalOnShatter = true;

    [Header("流程")]
    [Tooltip("碎裂触发后协程阻塞时间（秒）。只影响输入/后续逻辑等待，不影响粒子存活。\n此前你要求 0.1。")]
    [Range(0f, 1f)]
    public float PostShatterBlockDuration = 0.1f;

    [Header("运行时随机")]
    [Tooltip("每次播放前在基础参数上再叠一层随机（行列±1、力度浮动等）。关掉则每次裂纹几乎一致。")]
    public bool EnableRuntimeVariance = true;

    [Tooltip("运行时随机幅度倍率。1=默认；0=等效关闭浮动；>1 更夸张。")]
    [Range(0f, 2f)]
    public float RuntimeVarianceStrength = 1f;

    [Header("与战斗风格联动")]
    [Tooltip("启用后，数字键切换的 Battle Style 会用其 ShardCount 重算行列密度（Force/Lifetime 等仍用本页参数）。\n关闭则完全使用上方 Rows/Columns。")]
    public bool ScaleGridFromBattleStyle = true;

    public static CardFakeShatterTuning CreateDefault()
    {
        return new CardFakeShatterTuning();
    }

    public CardFakeShatterSettings ToRuntimeSettings()
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

    public void CopyFrom(CardFakeShatterSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        Rows = settings.Rows;
        Columns = settings.Columns;
        Force = settings.Force;
        InnerForce = settings.InnerForce;
        RandomForce = settings.RandomForce;
        Lifetime = settings.Lifetime;
        LifetimeRandom = settings.LifetimeRandom;
        Gravity = settings.Gravity;
        AngularVelocity = settings.AngularVelocity;
        HitDirectionBias = settings.HitDirectionBias;
        LateralSpread = settings.LateralSpread;
        FadeStart = settings.FadeStart;
        NoiseStrength = settings.NoiseStrength;
        GridJitter = settings.GridJitter;
        ShardSkipChance = settings.ShardSkipChance;
        PositionJitter = settings.PositionJitter;
        DirectionChaos = settings.DirectionChaos;
        SizeVariation = settings.SizeVariation;
        SpeedVariation = settings.SpeedVariation;
        HideOriginalOnShatter = settings.HideOriginalOnShatter;
        EnableFlash = settings.EnableFlash;
        FlashPeakAlpha = settings.FlashPeakAlpha;
        FlashMaxDuration = settings.FlashMaxDuration;
        PostShatterBlockDuration = settings.PostShatterBlockDuration;
        EnableRuntimeVariance = settings.EnableRuntimeVariance;
        RuntimeVarianceStrength = settings.RuntimeVarianceStrength;
    }
}
