using System.Collections.Generic;
using UnityEngine;

namespace QfPlayAgent.Samples.TableNine
{
    public static class TableNinePlayAgentCatalogFactory
    {
        public static PlayAgentCatalogConfig CreateDefault()
        {
            var catalog = ScriptableObject.CreateInstance<PlayAgentCatalogConfig>();
            catalog.RequireExplicitAllowlist = true;
            catalog.AssemblyNames = new List<string> { "TableNine.Runtime" };
            catalog.Commands = new List<PlayAgentCommandEntry>
            {
                Entry("DebugPingCommand", "Debug ping", "Health-check command for agent smoke tests.", QfAiRisk.Debug, 1),
                Entry("StartNewRunCommand", "Start new run", "Start a new run (player intent).", QfAiRisk.PlayerInput, 60),
                Entry("ClickBoardSlotCommand", "Click board slot", "Click a board slot by index (0-8).", QfAiRisk.PlayerInput, 15),
                Entry("ClickItemSlotCommand", "Click item slot", "Use help card in item slot (0-2).", QfAiRisk.PlayerInput, 15),
                Entry("PickHelpCardRewardCommand", "Pick help reward", "Choose a help card reward by card id.", QfAiRisk.PlayerInput, 20),
                Entry("SkipHelpRewardCommand", "Skip help reward", "Skip help card reward.", QfAiRisk.PlayerInput, 15),
                Entry("ChooseRoomCommand", "Choose room", "Pick a room by room id.", QfAiRisk.PlayerInput, 20),
                Entry("FinishDialogueCommand", "Finish dialogue", "Complete the active dialogue.", QfAiRisk.PlayerInput, 5)
            };
            return catalog;
        }

        private static PlayAgentCommandEntry Entry(
            string typeName,
            string displayName,
            string description,
            QfAiRisk risk,
            int defaultWaitFrames)
        {
            return new PlayAgentCommandEntry
            {
                CommandTypeName = typeName,
                DisplayName = displayName,
                Description = description,
                Risk = risk,
                Enabled = true,
                DefaultWaitFrames = defaultWaitFrames
            };
        }
    }
}
