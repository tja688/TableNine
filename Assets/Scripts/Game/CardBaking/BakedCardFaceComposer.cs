using UnityEngine;

public sealed class BakedCardFaceComposer
{
    private const float TemplateDistance = 6000f;
    private const float TemplateLocalCardHeight = 2f;

    private readonly GameObject mCardExampleTemplate;
    private readonly GameObject mPlayerCardTemplate;
    private Camera mCamera;
    private GameObject mCameraObject;
    private int mComposeIndex;

    public BakedCardFaceComposer(GameObject cardExampleTemplate = null, GameObject playerCardTemplate = null)
    {
        mCardExampleTemplate = BakedCardPrefabRefs.ResolveCardExample(cardExampleTemplate);
        mPlayerCardTemplate = BakedCardPrefabRefs.ResolvePlayerCard(playerCardTemplate);
    }

    public BakedCardSpriteSet ComposeSet(BakedCardFaceRenderData data, float pixelsPerUnit)
    {
        if (data == null)
        {
            return default;
        }

        var template = LoadTemplate(data.Template);
        if (template == null)
        {
            Debug.LogWarning($"Baked card face template is missing: {data.Template}");
            return default;
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
            var contentWorldHeight = ResolveTemplateContentWorldHeight(root);
            var framingBounds = CalculateSpriteBounds(root);
            if (framingBounds.size.x <= 0f || framingBounds.size.y <= 0f)
            {
                framingBounds = new Bounds(root.transform.position, new Vector3(1.5f, TemplateLocalCardHeight, 0.1f));
            }

            var faceSprite = RenderFramedView(
                root,
                data.Size,
                pixelsPerUnit,
                framingBounds,
                includeFaceElements: true,
                $"BakedCardFace_{data.Size}_{mComposeIndex}");

            var backSprite = RenderFramedView(
                root,
                data.Size,
                pixelsPerUnit,
                framingBounds,
                includeFaceElements: false,
                $"BakedCardBack_{data.Size}_{mComposeIndex}");

            return new BakedCardSpriteSet(faceSprite, backSprite, contentWorldHeight, 1f);
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

    public Sprite Compose(BakedCardFaceRenderData data, float pixelsPerUnit, out Sprite backSprite)
    {
        var spriteSet = ComposeSet(data, pixelsPerUnit);
        backSprite = spriteSet.BackSprite;
        return spriteSet.FaceSprite;
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

    private static float ResolveTemplateContentWorldHeight(GameObject root)
    {
        var collider = root.GetComponent<Collider2D>();
        if (collider != null && collider.bounds.size.y > 0f)
        {
            return collider.bounds.size.y;
        }

        return TemplateLocalCardHeight;
    }

    private Sprite RenderFramedView(
        GameObject root,
        BakedCardFaceSize size,
        float pixelsPerUnit,
        Bounds framingBounds,
        bool includeFaceElements,
        string textureName)
    {
        ConfigureTemplateVisibility(root.transform, includeFaceElements);

        var dimensions = GetPixelDimensions(size);
        var padding = Mathf.Max(framingBounds.size.x, framingBounds.size.y) * 0.05f;
        var aspect = (float)dimensions.x / dimensions.y;
        var halfHeight = framingBounds.extents.y + padding;
        var halfWidthByHeight = halfHeight * aspect;
        var halfWidth = framingBounds.extents.x + padding;
        if (halfWidthByHeight < halfWidth)
        {
            halfHeight = halfWidth / aspect;
        }

        mCamera.aspect = aspect;
        mCamera.orthographicSize = halfHeight;
        mCamera.transform.position = new Vector3(framingBounds.center.x, framingBounds.center.y, framingBounds.center.z - 10f);
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
            name = textureName
        };
        texture.ReadPixels(new Rect(0, 0, dimensions.x, dimensions.y), 0, 0);
        texture.Apply(false, false);

        mCamera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        renderTexture.Release();
        if (Application.isPlaying)
        {
            Object.Destroy(renderTexture);
        }
        else
        {
            Object.DestroyImmediate(renderTexture);
        }

        var croppedTexture = CropToOpaqueBounds(texture, out _);
        if (croppedTexture != texture)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(texture);
            }
            else
            {
                Object.DestroyImmediate(texture);
            }
        }

        var sprite = Sprite.Create(
            croppedTexture,
            new Rect(0f, 0f, croppedTexture.width, croppedTexture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
        sprite.name = croppedTexture.name;
        return sprite;
    }

    private static void ConfigureTemplateVisibility(Transform root, bool includeFaceElements)
    {
        SetActiveIfExists(root, "CardFront", includeFaceElements);
        SetActiveIfExists(root, "CardTextCanvas", includeFaceElements);
        SetActiveIfExists(root, "CardName", includeFaceElements);
        SetActiveIfExists(root, "CardBack", !includeFaceElements);
    }

    private static void SetActiveIfExists(Transform root, string childName, bool active)
    {
        var child = FindChild(root, childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private static Texture2D CropToOpaqueBounds(Texture2D source, out RectInt cropRect)
    {
        cropRect = new RectInt(0, 0, source.width, source.height);
        if (source == null || source.width <= 0 || source.height <= 0)
        {
            return source;
        }

        var pixels = source.GetPixels32();
        var width = source.width;
        var height = source.height;
        var minX = width;
        var minY = height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a <= 0)
                {
                    continue;
                }

                if (x < minX)
                {
                    minX = x;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return source;
        }

        var cropWidth = maxX - minX + 1;
        var cropHeight = maxY - minY + 1;
        cropRect = new RectInt(minX, minY, cropWidth, cropHeight);

        if (cropWidth == width && cropHeight == height)
        {
            return source;
        }

        var cropped = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, false)
        {
            filterMode = source.filterMode,
            wrapMode = source.wrapMode,
            name = source.name
        };

        var croppedPixels = new Color32[cropWidth * cropHeight];
        for (var y = 0; y < cropHeight; y++)
        {
            for (var x = 0; x < cropWidth; x++)
            {
                croppedPixels[y * cropWidth + x] = pixels[(minY + y) * width + (minX + x)];
            }
        }

        cropped.SetPixels32(croppedPixels);
        cropped.Apply(false, false);
        return cropped;
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
