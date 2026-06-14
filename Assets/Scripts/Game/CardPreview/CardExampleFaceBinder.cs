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

        var showStats = card.CardType == CardType.Monster;
        SetText(root, "CardName", card.DisplayName ?? string.Empty);
        SetStatText(root, "CardLife", showStats, card.BaseHp.ToString());
        SetStatText(root, "CardAttack", showStats, card.BaseAttack.ToString());
        SetStatText(root, "CardDefense", showStats, card.BaseArmor.ToString());

        SetActive(root, "CardLifeIcon", showStats);
        SetActive(root, "CardAttackIcon", showStats);
        SetActive(root, "CardDefenseIcon", showStats);

        ApplyDeckSprites(root, card, config);
        ApplyMainIcon(root, card, config);
        ApplyEntryIcons(root, card, config, showStats);
    }

    private static void ApplyDeckSprites(Transform root, CardDefinition card, IConfigModel config)
    {
        ApplySprite(root, "CardFront", config.GetEffectiveCardFaceImage(card.CardId));
        ApplySprite(root, "CardBack", config.GetEffectiveCardBackImage(card.CardId));
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

    private static void ApplyMainIcon(Transform root, CardDefinition card, IConfigModel config)
    {
        var mainIcon = FindChild(root, "CardMainIcon");
        if (mainIcon == null)
        {
            return;
        }

        mainIcon.gameObject.SetActive(true);
        var renderer = mainIcon.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        var sprite = config.GetCardMainImage(card.CardId);
        renderer.sprite = sprite;
        renderer.enabled = sprite != null;
    }

    private static void ApplyEntryIcons(Transform root, CardDefinition card, IConfigModel config, bool showStats)
    {
        var container = FindChild(root, "EntryIcons");
        if (container == null)
        {
            return;
        }

        if (!showStats || card.SkillIds == null || card.SkillIds.Count == 0)
        {
            container.gameObject.SetActive(false);
            return;
        }

        container.gameObject.SetActive(true);
        var skillSprites = new List<Sprite>();
        for (var i = 0; i < card.SkillIds.Count; i++)
        {
            var skill = config.GetSkillDefinition(card.SkillIds[i]);
            if (skill?.Image != null)
            {
                skillSprites.Add(skill.Image);
            }
        }

        for (var i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            var hasSprite = i < skillSprites.Count;
            child.gameObject.SetActive(hasSprite);
            if (!hasSprite)
            {
                continue;
            }

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = skillSprites[i];
                renderer.enabled = skillSprites[i] != null;
            }
        }
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
}
