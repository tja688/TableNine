using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

[CreateAssetMenu(fileName = "TableNineUIPanelRegistry", menuName = "TableNine/UI Panel Registry")]
public sealed class TableNineUIPanelRegistry : ScriptableObject
{
    [Serializable]
    public sealed class PanelEntry
    {
        public string PanelName;
        public GameObject Prefab;
    }

    [SerializeField] private List<PanelEntry> mPanels = new List<PanelEntry>();

    public GameObject GetPanelPrefab(PanelSearchKeys keys)
    {
        var explicitName = keys.GameObjName;
        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            return GetPanelPrefab(explicitName);
        }

        return keys.PanelType != null ? GetPanelPrefab(keys.PanelType.Name) : null;
    }

    public GameObject GetPanelPrefab(string panelName)
    {
        for (var i = 0; i < mPanels.Count; i++)
        {
            var entry = mPanels[i];
            if (entry == null || entry.Prefab == null)
            {
                continue;
            }

            if (entry.PanelName == panelName || entry.Prefab.name == panelName)
            {
                return entry.Prefab;
            }
        }

        return null;
    }
}
