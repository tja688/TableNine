using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class CardExampleFaceBinder
{
    public static void Apply(Transform root, CardDefinition card, IConfigModel config)
    {
        if (root == null || card == null || config == null)
        {
            return;
        }

        Apply(root, BakedCardRenderDataFactory.CreateFromCardDefinition(config, card));
    }

    public static void Apply(Transform root, BakedCardFaceRenderData data)
    {
        if (root == null || data == null)
        {
            return;
        }

        var showStats = data.StatMode == BakedCardFaceStatMode.FullStats;
        SetText(root, "CardName", data.DisplayName ?? string.Empty);
        SetStatText(root, "CardLife", showStats, data.Life.ToString());
        SetStatText(root, "CardAttack", showStats, data.Attack.ToString());
        SetStatText(root, "CardDefense", showStats, data.Defense.ToString());

        SetActive(root, "CardLifeIcon", showStats);
        SetActive(root, "CardAttackIcon", showStats);
        SetActive(root, "CardDefenseIcon", showStats);

        ApplySprite(root, "CardFront", data.FaceSprite);
        ApplySprite(root, "CardBack", data.BackSprite);
        ApplyMainIcon(root, data.MainIconSprite);
        ApplyIconContainer(root, "EntryIcons", data);
        ApplyIconContainer(root, "SkillIcons", data);
    }

    private static void SetStatText(Transform root, string childName, bool visible, string value)
    {
        var child = FindChild(root, childName);
        if (child == null)
        {
            return;
        }

        child.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        SetText(root, childName, value);
    }

    private static void SetText(Transform root, string childName, string value)
    {
        var child = FindChild(root, childName);
        if (child == null)
        {
            return;
        }

        var text = child.GetComponent<TMP_Text>();
        if (text == null)
        {
            return;
        }

        text.text = value ?? string.Empty;
        text.enableWordWrapping = false;
        text.ForceMeshUpdate(true, true);
    }

    private static void SetActive(Transform root, string childName, bool active)
    {
        var child = FindChild(root, childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    public static Transform FindChild(Transform root, string childName)
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

    private static void ApplySprite(Transform root, string childName, Sprite sprite)
    {
        var child = FindChild(root, childName);
        if (child == null)
        {
            return;
        }

        var renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        if (sprite != null)
        {
            renderer.sprite = sprite;
        }

        renderer.enabled = renderer.sprite != null;
    }

    private static void ApplyMainIcon(Transform root, Sprite sprite)
    {
        var mainIcon = FindChild(root, "CardMainIcon");
        if (mainIcon == null)
        {
            return;
        }

        var renderer = mainIcon.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.sprite = sprite;
        renderer.enabled = sprite != null;
        mainIcon.gameObject.SetActive(sprite != null);
    }

    private static void ApplyIconContainer(Transform root, string containerName, BakedCardFaceRenderData data)
    {
        var container = FindChild(root, containerName);
        if (container == null)
        {
            return;
        }

        var sprites = data.EntryIconSprites;
        var showIcons = data.StatMode == BakedCardFaceStatMode.FullStats
                        && ((sprites != null && sprites.Count > 0) || data.StampedIconCount > 0);
        container.gameObject.SetActive(showIcons);
        if (!showIcons)
        {
            HideIconChildren(container);
            return;
        }

        for (var i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            var shouldShow = sprites != null && i < sprites.Count;
            if (!shouldShow && sprites != null && sprites.Count > 0)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            shouldShow = i < data.StampedIconCount || shouldShow;
            child.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            if (sprites == null || i >= sprites.Count)
            {
                continue;
            }

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = sprites[i];
                renderer.enabled = sprites[i] != null;
            }
        }
    }

    private static void HideIconChildren(Transform container)
    {
        for (var i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            child.gameObject.SetActive(false);

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                continue;
            }

            renderer.sprite = null;
            renderer.enabled = false;
        }
    }
}
