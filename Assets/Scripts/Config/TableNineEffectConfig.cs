using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TableNineEffectConfig", menuName = "TableNine/Game Config/Effect Config")]
public sealed class TableNineEffectConfig : ScriptableObject
{
    public List<EffectGraphDefinition> EffectGraphs = new List<EffectGraphDefinition>();
    public List<HelpCardEffectMapping> HelpCardEffectMappings = new List<HelpCardEffectMapping>();
}
