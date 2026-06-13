using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TableNineRelicConfig", menuName = "TableNine/Game Config/Relic Config")]
public sealed class TableNineRelicConfig : ScriptableObject
{
    public List<RelicDefinition> Relics = new List<RelicDefinition>();
    public List<RoomDefinition> Rooms = new List<RoomDefinition>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        DescriptionPanelConfigValidation.SanitizeRelicConfig(this);
    }
#endif
}
