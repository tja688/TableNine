using UnityEngine;

public static class BakedCardRuntimeSpriteUtility
{
    public static bool IsRuntimeGenerated(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        var texture = sprite.texture;
        return texture != null
               && (sprite.name.StartsWith("BakedCardFace_") || sprite.name.StartsWith("BakedCardBack_"))
               && (texture.name.StartsWith("BakedCardFace_") || texture.name.StartsWith("BakedCardBack_"));
    }

    public static void Release(ref Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        var texture = sprite.texture;
        if (!IsRuntimeGenerated(sprite))
        {
            sprite = null;
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(sprite);
            if (texture != null)
            {
                Object.Destroy(texture);
            }
        }
        else
        {
            Object.DestroyImmediate(sprite);
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }

        sprite = null;
    }
}
