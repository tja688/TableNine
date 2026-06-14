using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class BakedCardFaceComposer
{
    private const string CardExamplePath = "Assets/Prefabs/Cards/CardExample.prefab";
    private const string PlayerCardPath = "Assets/Prefabs/Cards/PlayerCard.prefab";
    private const float TemplateDistance = 6000f;

    private readonly GameObject mCardExampleTemplate;
    private readonly GameObject mPlayerCardTemplate;
    private Camera mCamera;
    private GameObject mCameraObject;
    private int mComposeIndex;

    public BakedCardFaceComposer(GameObject cardExampleTemplate = null, GameObject playerCardTemplate = null)
    {
#if UNITY_EDITOR
        mCardExampleTemplate = cardExampleTemplate != null
            ? cardExampleTemplate
            : AssetDatabase.LoadAssetAtPath<GameObject>(CardExamplePath);
        mPlayerCardTemplate = playerCardTemplate != null
            ? playerCardTemplate
            : AssetDatabase.LoadAssetAtPath<GameObject>(PlayerCardPath);
#else
        mCardExampleTemplate = cardExampleTemplate;
        mPlayerCardTemplate = playerCardTemplate;
#endif
    }

    public Sprite Compose(BakedCardFaceRenderData data, float pixelsPerUnit, out Sprite backSprite)
    {
        backSprite = null;
        if (data == null)
        {
            return null;
        }

        var template = LoadTemplate(data.Template);
        if (template == null)
        {
            Debug.LogWarning($"Baked card face template is missing: {data.Template}");
            return null;
        }

        EnsureCamera();

        var root = Object.Instantiate(template);
        root.name = $"__BakedCardFaceTemplate_{mComposeIndex++}";
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.position = new Vector3(TemplateDistance, TemplateDistance, 0f);
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        try
        {
            ApplyData(root.transform, data);
            backSprite = FindChild(root.transform, "CardBack")?.GetComponent<SpriteRenderer>()?.sprite;
            return RenderTemplate(root, data.Size, pixelsPerUnit);
        }
        finally
        {
            if (Application.isPlaying)
            {
                Object.Destroy(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    public void Dispose()
    {
        if (mCameraObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(mCameraObject);
        }
        else
        {
            Object.DestroyImmediate(mCameraObject);
        }

        mCamera = null;
        mCameraObject = null;
    }

    private GameObject LoadTemplate(BakedCardFaceTemplate template)
    {
        return template == BakedCardFaceTemplate.PlayerCard
            ? mPlayerCardTemplate
            : mCardExampleTemplate;
    }

    private void EnsureCamera()
    {
        if (mCamera != null)
        {
            return;
        }

        mCameraObject = new GameObject("__BakedCardFaceCamera");
        mCameraObject.hideFlags = HideFlags.HideAndDontSave;
        mCamera = mCameraObject.AddComponent<Camera>();
        mCamera.enabled = false;
        mCamera.clearFlags = CameraClearFlags.SolidColor;
        mCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        mCamera.orthographic = true;
        mCamera.nearClipPlane = 0.01f;
        mCamera.farClipPlane = 100f;
        mCamera.allowHDR = false;
        mCamera.allowMSAA = false;
    }

    private static void ApplyData(Transform root, BakedCardFaceRenderData data)
    {
        CardExampleFaceBinder.Apply(root, data);
    }

    private Sprite RenderTemplate(GameObject root, BakedCardFaceSize size, float pixelsPerUnit)
    {
        var dimensions = GetPixelDimensions(size);
        var bounds = CalculateSpriteBounds(root);
        if (bounds.size.x <= 0f || bounds.size.y <= 0f)
        {
            bounds = new Bounds(root.transform.position, new Vector3(2f, 3f, 0.1f));
        }

        var padding = Mathf.Max(bounds.size.x, bounds.size.y) * 0.05f;
        var aspect = (float)dimensions.x / dimensions.y;
        var halfHeight = bounds.extents.y + padding;
        var halfWidthByHeight = halfHeight * aspect;
        var halfWidth = bounds.extents.x + padding;
        if (halfWidthByHeight < halfWidth)
        {
            halfHeight = halfWidth / aspect;
        }

        mCamera.aspect = aspect;
        mCamera.orthographicSize = halfHeight;
        mCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 10f);
        mCamera.transform.rotation = Quaternion.identity;

        var renderTexture = new RenderTexture(dimensions.x, dimensions.y, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var previousTarget = mCamera.targetTexture;
        var previousActive = RenderTexture.active;

        mCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        mCamera.Render();

        var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"BakedCardFace_{size}_{mComposeIndex}"
        };
        texture.ReadPixels(new Rect(0, 0, dimensions.x, dimensions.y), 0, 0);
        texture.Apply(false, false);

        mCamera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        renderTexture.Release();
        Object.Destroy(renderTexture);

        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
        sprite.name = texture.name;
        return sprite;
    }

    private static Vector2Int GetPixelDimensions(BakedCardFaceSize size)
    {
        switch (size)
        {
            case BakedCardFaceSize.Small:
                return new Vector2Int(96, 144);
            case BakedCardFaceSize.Large:
                return new Vector2Int(256, 384);
            default:
                return new Vector2Int(160, 240);
        }
    }

    private static Bounds CalculateSpriteBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        var bounds = new Bounds(root.transform.position, Vector3.zero);
        var hasBounds = false;
        for (var i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].gameObject.activeInHierarchy || renderers[i].sprite == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return bounds;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var result = FindChild(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
