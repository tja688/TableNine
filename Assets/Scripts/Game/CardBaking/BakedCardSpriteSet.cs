using UnityEngine;

public readonly struct BakedCardSpriteSet
{
    public BakedCardSpriteSet(Sprite faceSprite, Sprite backSprite)
        : this(faceSprite, backSprite, 0f, 1f)
    {
    }

    public BakedCardSpriteSet(
        Sprite faceSprite,
        Sprite backSprite,
        float contentWorldHeight,
        float contentFillRatio)
    {
        FaceSprite = faceSprite;
        BackSprite = backSprite;
        ContentWorldHeight = contentWorldHeight;
        ContentFillRatio = Mathf.Clamp(contentFillRatio, 0.1f, 1f);
    }

    public Sprite FaceSprite { get; }
    public Sprite BackSprite { get; }
    public float ContentWorldHeight { get; }
    public float ContentFillRatio { get; }
    public bool HasFace => FaceSprite != null;
}
