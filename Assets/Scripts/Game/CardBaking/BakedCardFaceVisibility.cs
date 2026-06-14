using UnityEngine;

public static class BakedCardFaceVisibility
{
    public static bool IsFaceVisible(float pivotLocalEulerY)
    {
        var y = Mathf.Repeat(pivotLocalEulerY, 360f);
        return y <= 90f || y >= 270f;
    }

    public static void Apply(
        Transform visualPivot,
        SpriteRenderer faceRenderer,
        SpriteRenderer backRenderer,
        bool? forceFaceVisible = null)
    {
        if (faceRenderer == null || backRenderer == null)
        {
            return;
        }

        var faceVisible = forceFaceVisible ?? (visualPivot == null || IsFaceVisible(visualPivot.localEulerAngles.y));
        faceRenderer.enabled = faceVisible && faceRenderer.sprite != null;
        backRenderer.enabled = !faceVisible && backRenderer.sprite != null;
    }
}
