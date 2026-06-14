using UnityEngine;

public readonly struct BakedCardSpriteSet
{
    public BakedCardSpriteSet(Sprite faceSprite, Sprite backSprite)
    {
        FaceSprite = faceSprite;
        BackSprite = backSprite;
    }

    public Sprite FaceSprite { get; }
    public Sprite BackSprite { get; }
    public bool HasFace => FaceSprite != null;
}
