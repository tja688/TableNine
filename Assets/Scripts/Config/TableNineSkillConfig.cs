using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TableNineSkillConfig", menuName = "TableNine/Game Config/Skill Config")]
public sealed class TableNineSkillConfig : ScriptableObject
{
    public List<SkillDefinition> Skills = new List<SkillDefinition>();
    public List<SkillEffectBinding> SkillBindings = new List<SkillEffectBinding>();
    public List<SkillBehaviorRule> SkillBehaviorRules = new List<SkillBehaviorRule>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        DescriptionPanelConfigValidation.SanitizeSkillConfig(this);
    }
#endif
}
