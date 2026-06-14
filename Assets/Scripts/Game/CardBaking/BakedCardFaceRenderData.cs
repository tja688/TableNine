using UnityEngine;

public enum BakedCardFaceTemplate
{
    CardExample,
    PlayerCard
}

public enum BakedCardFaceSize
{
    Small,
    Medium,
    Large
}

public enum BakedCardFaceStatMode
{
    FullStats,
    MainIconOnly
}

public sealed class BakedCardFaceRenderData
{
    public string DisplayName;
    public int Life;
    public int Attack;
    public int Defense;
    public BakedCardFaceTemplate Template;
    public BakedCardFaceSize Size;
    public BakedCardFaceStatMode StatMode;
    public int StampedIconCount;
}
