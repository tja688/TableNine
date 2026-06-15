using UnityEngine;

/// <summary>
/// 主相机 Image Effect：仅对世界空间渲染结果做实时模糊，UI 不受影响。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class BackgroundBlurCameraEffect : MonoBehaviour
{
    private static readonly int SharpTexId = Shader.PropertyToID("_SharpTex");
    private static readonly int BlurOffsetId = Shader.PropertyToID("_BlurOffset");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private BackgroundBlurHost mHost;
    private Material mMaterial;

    internal void Bind(BackgroundBlurHost host)
    {
        mHost = host;
        mMaterial = host != null ? host.BlurMaterial : null;
        enabled = false;
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (mHost == null || mMaterial == null || !mHost.ShouldRenderBlur)
        {
            Graphics.Blit(src, dest);
            return;
        }

        var width = Mathf.Max(1, src.width / 2);
        var height = Mathf.Max(1, src.height / 2);
        var tempA = RenderTexture.GetTemporary(width, height, 0, src.format);
        var tempB = RenderTexture.GetTemporary(width, height, 0, src.format);

        Graphics.Blit(src, tempA);
        mMaterial.SetFloat(BlurOffsetId, mHost.BlurSize);

        const int iterations = 2;
        for (var i = 0; i < iterations; i++)
        {
            Graphics.Blit(tempA, tempB, mMaterial, 0);
            Graphics.Blit(tempB, tempA, mMaterial, 1);
        }

        mMaterial.SetTexture(SharpTexId, src);
        mMaterial.SetFloat(IntensityId, mHost.DisplayedIntensity);
        Graphics.Blit(tempA, dest, mMaterial, 2);

        RenderTexture.ReleaseTemporary(tempA);
        RenderTexture.ReleaseTemporary(tempB);
    }
}
