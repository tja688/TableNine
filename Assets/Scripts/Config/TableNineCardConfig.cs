using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TableNineCardConfig", menuName = "TableNine/Game Config/Card Config")]
public sealed class TableNineCardConfig : ScriptableObject
{
    public List<CardDeckDefinition> CardDecks = new List<CardDeckDefinition>();
    public List<CardDefinition> Cards = new List<CardDefinition>();
    public List<MonsterDeckRuleDefinition> MonsterDeckRules = new List<MonsterDeckRuleDefinition>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        DescriptionPanelConfigValidation.SanitizeCardConfig(this);
    }
#endif
}
