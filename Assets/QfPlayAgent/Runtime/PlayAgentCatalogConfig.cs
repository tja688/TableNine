using System;
using System.Collections.Generic;
using UnityEngine;

namespace QfPlayAgent
{
    [Serializable]
    public sealed class PlayAgentCommandEntry
    {
        public string CommandTypeName;
        public string DisplayName;
        public string Description;
        public QfAiRisk Risk = QfAiRisk.PlayerInput;
        public bool Enabled = true;
        public int DefaultWaitFrames = 1;
    }

    [CreateAssetMenu(fileName = "PlayAgentCatalog", menuName = "QfPlayAgent/Command Catalog")]
    public sealed class PlayAgentCatalogConfig : ScriptableObject
    {
        [Tooltip("When true, only commands listed below (or tagged with QfAiAction) are exposed.")]
        public bool RequireExplicitAllowlist = true;

        [Tooltip("Optional assembly name filters. Empty means scan all loaded assemblies.")]
        public List<string> AssemblyNames = new List<string>();

        public List<PlayAgentCommandEntry> Commands = new List<PlayAgentCommandEntry>();
    }
}
