using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TableNineCharacterConfig", menuName = "TableNine/Game Config/Character Config")]
public sealed class TableNineCharacterConfig : ScriptableObject
{
    public List<CharacterDefinition> Characters = new List<CharacterDefinition>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        DescriptionPanelConfigValidation.SanitizeCharacterConfig(this);
    }
#endif
}
