using System.Collections.Generic;
using QFramework;
using UnityEngine;

/// <summary>
/// 临时测试：按空格随机获得遗物或技能，用于验证 UIGameplayPanel 侧栏悬停描述。
/// 挂载在场景任意对象上即可，不影响正式流程时可整对象删除。
/// </summary>
public sealed class GameplaySidePanelDebugDemo : MonoBehaviour, IController
{
    [SerializeField] private KeyCode mTriggerKey = KeyCode.Space;
    [SerializeField] private bool mAutoStartRun = true;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(mTriggerKey))
        {
            return;
        }

        if (!TableNine.IsInitialized)
        {
            TableNine.InitArchitecture();
        }

        if (mAutoStartRun && !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            this.SendCommand(new StartNewRunCommand());
        }

        if (!this.GetModel<IRunModel>().IsRunActive.Value)
        {
            Debug.LogWarning("[GameplaySidePanelDebugDemo] Run is not active; cannot add relic/skill.");
            return;
        }

        if (Random.value < 0.5f)
        {
            TryAddRandomRelic();
        }
        else
        {
            TryAddRandomSkill();
        }
    }

    private void TryAddRandomRelic()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();
        var relicSystem = this.GetSystem<IRelicSystem>();
        if (playerModel.Relics.Count >= playerModel.MaxRelicCount)
        {
            Debug.LogWarning("[GameplaySidePanelDebugDemo] Relic slots are full.");
            return;
        }

        var pool = new List<string>();

        foreach (var relic in configModel.GetAllRelicDefinitions())
        {
            if (relic == null || relic.ExcludeFromPool || string.IsNullOrWhiteSpace(relic.RelicId))
            {
                continue;
            }

            if (!playerModel.HasRelic(relic.RelicId))
            {
                pool.Add(relic.RelicId);
            }
        }

        if (pool.Count == 0)
        {
            Debug.LogWarning("[GameplaySidePanelDebugDemo] No available relic to add.");
            return;
        }

        var relicId = pool[Random.Range(0, pool.Count)];
        if (!relicSystem.AddRelic(relicId))
        {
            Debug.LogWarning($"[GameplaySidePanelDebugDemo] Failed to add relic: {relicId}");
            return;
        }

        var definition = configModel.GetRelicDefinition(relicId);
        Debug.Log($"[GameplaySidePanelDebugDemo] Added relic: {definition?.DisplayName ?? relicId}");
    }

    private void TryAddRandomSkill()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();
        var pool = new List<string>();

        foreach (var skill in configModel.GetAllSkillDefinitions())
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
            {
                continue;
            }

            if (!HasSkill(playerModel, skill.SkillId))
            {
                pool.Add(skill.SkillId);
            }
        }

        if (pool.Count == 0)
        {
            Debug.LogWarning("[GameplaySidePanelDebugDemo] No available skill to add.");
            return;
        }

        var skillId = pool[Random.Range(0, pool.Count)];
        playerModel.AddSkill(skillId);
        var definition = configModel.GetSkillDefinition(skillId);
        Debug.Log($"[GameplaySidePanelDebugDemo] Added skill: {definition?.DisplayName ?? skillId}");
    }

    private static bool HasSkill(IPlayerModel playerModel, string skillId)
    {
        for (var i = 0; i < playerModel.SkillIds.Count; i++)
        {
            if (playerModel.SkillIds[i] == skillId)
            {
                return true;
            }
        }

        return false;
    }
}
