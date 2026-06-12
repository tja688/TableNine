using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

[CreateAssetMenu(fileName = "TableNineUIPanelRegistry", menuName = "TableNine/UI Panel Registry")]
public sealed class TableNineUIPanelRegistry : ScriptableObject, ISerializationCallbackReceiver
{
    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        mRuntimeEntries = null;
    }

    [Serializable]
    public sealed class PanelEntry
    {
#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [LabelText("UI Key")]
#endif
        public string UIKey;

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [LabelText("UI Type")]
#endif
        public TableNineUIType UIType;

#if ODIN_INSPECTOR
        [BoxGroup("Panel")]
        [LabelText("UIKit Panel Name")]
#endif
        public string PanelName;

#if ODIN_INSPECTOR
        [BoxGroup("Panel")]
        [LabelText("Formal Prefab")]
        [AssetsOnly]
#endif
        public GameObject Prefab;

#if ODIN_INSPECTOR
        [BoxGroup("Panel")]
        [LabelText("UI Level")]
#endif
        public UILevel Level = UILevel.PopUI;

#if ODIN_INSPECTOR
        [BoxGroup("Panel")]
        [LabelText("Open Type")]
#endif
        public PanelOpenType OpenType = PanelOpenType.Single;

#if ODIN_INSPECTOR
        [BoxGroup("Fallback")]
        [LabelText("Fallback")]
#endif
        public TableNineUIFallbackStrategy FallbackStrategy = TableNineUIFallbackStrategy.AtomicChoiceList;

#if ODIN_INSPECTOR
        [BoxGroup("Fallback")]
        [LabelText("Blocks Input")]
#endif
        public bool BlocksGameplayInput = true;

#if ODIN_INSPECTOR
        [BoxGroup("Notes")]
        [TextArea(2, 5)]
        [HideLabel]
#endif
        public string Notes;

        public bool HasFormalPrefab => Prefab != null;

        public PanelEntry Clone()
        {
            return new PanelEntry
            {
                UIKey = UIKey,
                UIType = UIType,
                PanelName = PanelName,
                Prefab = Prefab,
                Level = Level,
                OpenType = OpenType,
                FallbackStrategy = FallbackStrategy,
                BlocksGameplayInput = BlocksGameplayInput,
                Notes = Notes
            };
        }
    }

#if ODIN_INSPECTOR
    [Title("TableNine UI Binding Registry")]
    [InfoBox("Gameplay emits UI needs through events. This registry maps stable UI keys to formal UIKit prefabs and a controlled fallback strategy.")]
    [TableList(AlwaysExpanded = true)]
#endif
    [SerializeField] private List<PanelEntry> mPanels = new List<PanelEntry>();

    // ── Fallback 组件化预制件引用 ──
#if ODIN_INSPECTOR
    [Title("Fallback Component Prefabs")]
    [InfoBox("组件化预制件，供 Fallback 面板运行时实例化。为空时回退到纯代码生成。")]
    [PropertyOrder(100)]
#endif
    [SerializeField] private GameObject mFallbackButtonPrefab;
#if ODIN_INSPECTOR
    [PropertyOrder(101)]
#endif
    [SerializeField] private GameObject mFallbackTextPrefab;
#if ODIN_INSPECTOR
    [PropertyOrder(102)]
#endif
    [SerializeField] private GameObject mFallbackIconPrefab;
#if ODIN_INSPECTOR
    [PropertyOrder(103)]
#endif
    [SerializeField] private GameObject mFallbackPanelPrefab;
#if ODIN_INSPECTOR
    [PropertyOrder(104)]
#endif
    [SerializeField] private GameObject mFallbackScrollViewPrefab;

#if ODIN_INSPECTOR
    [Title("DescriptionPanel")]
    [InfoBox("UIGameplayPanel / DescriptionPanel 单行描述文案配置。")]
    [PropertyOrder(110)]
#endif
    [SerializeField] private TableNineDescriptionPanelConfig mDescriptionPanelConfig;

    public GameObject FallbackButtonPrefab => mFallbackButtonPrefab;
    public GameObject FallbackTextPrefab => mFallbackTextPrefab;
    public GameObject FallbackIconPrefab => mFallbackIconPrefab;
    public GameObject FallbackPanelPrefab => mFallbackPanelPrefab;
    public GameObject FallbackScrollViewPrefab => mFallbackScrollViewPrefab;
    public TableNineDescriptionPanelConfig DescriptionPanelConfig => mDescriptionPanelConfig;

    public void SetDescriptionPanelConfig(TableNineDescriptionPanelConfig config)
    {
        mDescriptionPanelConfig = config;
    }

    private List<PanelEntry> mRuntimeEntries;

    public IReadOnlyList<PanelEntry> Panels => EnsureRuntimeEntries();

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
        var entries = EnsureRuntimeEntries();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
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

    public PanelEntry GetEntry(string uiKey)
    {
        var entries = EnsureRuntimeEntries();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry != null && entry.UIKey == uiKey)
            {
                return entry;
            }
        }

        return null;
    }

    public PanelEntry GetEntryOrDefault(string uiKey)
    {
        return GetEntry(uiKey) ?? CreateMissingEntry(uiKey);
    }

#if ODIN_INSPECTOR
    [Button(ButtonSizes.Medium, Name = "打开 UI 面板管理窗口")]
    [PropertyOrder(-10)]
#endif
    public void OpenRegistryEditorWindow()
    {
#if UNITY_EDITOR
        var type = System.Type.GetType("TableNineUI.Editor.TableNineUIRegistryEditorWindow, Assembly-CSharp-Editor");
        if (type == null) return;
        var openMethod = type.GetMethod("OpenWithRegistry", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        openMethod?.Invoke(null, new object[] { this });
#endif
    }

#if ODIN_INSPECTOR
    [Button(ButtonSizes.Medium)]
#endif
    public void ApplyRecommendedDefaults()
    {
        var defaults = CreateRecommendedDefaults();
        for (var i = 0; i < defaults.Count; i++)
        {
            var existing = FindSerializedByKeyOrPanel(defaults[i].UIKey, defaults[i].PanelName);
            if (existing == null)
            {
                mPanels.Add(defaults[i]);
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.UIKey))
            {
                existing.UIKey = defaults[i].UIKey;
            }

            if (existing.UIType == default)
            {
                existing.UIType = defaults[i].UIType;
            }

            if (string.IsNullOrWhiteSpace(existing.PanelName))
            {
                existing.PanelName = defaults[i].PanelName;
            }

            if (existing.FallbackStrategy == default && defaults[i].FallbackStrategy != default)
            {
                existing.FallbackStrategy = defaults[i].FallbackStrategy;
            }

            existing.Level = defaults[i].Level;
            existing.OpenType = defaults[i].OpenType;
            existing.BlocksGameplayInput = defaults[i].BlocksGameplayInput;
            if (string.IsNullOrWhiteSpace(existing.Notes))
            {
                existing.Notes = defaults[i].Notes;
            }
        }

        mRuntimeEntries = null;
    }

    private List<PanelEntry> EnsureRuntimeEntries()
    {
        if (mRuntimeEntries != null && mRuntimeEntries.Count > 0)
        {
            return mRuntimeEntries;
        }

        mRuntimeEntries = new List<PanelEntry>();
        for (var i = 0; i < mPanels.Count; i++)
        {
            if (mPanels[i] != null)
            {
                mRuntimeEntries.Add(mPanels[i]);
            }
        }

        var defaults = CreateRecommendedDefaults();
        for (var i = 0; i < defaults.Count; i++)
        {
            if (FindRuntimeByKey(defaults[i].UIKey) != null)
            {
                continue;
            }

            var defaultEntry = defaults[i].Clone();
            var existingPanelEntry = FindRuntimeByPanel(defaults[i].PanelName);
            if (existingPanelEntry != null && existingPanelEntry.Prefab != null)
            {
                defaultEntry.Prefab = existingPanelEntry.Prefab;
            }

            mRuntimeEntries.Add(defaultEntry);
        }

        return mRuntimeEntries;
    }

    private PanelEntry FindSerializedByKeyOrPanel(string uiKey, string panelName)
    {
        for (var i = 0; i < mPanels.Count; i++)
        {
            var entry = mPanels[i];
            if (entry == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(uiKey) && entry.UIKey == uiKey)
            {
                return entry;
            }

            if (!string.IsNullOrWhiteSpace(panelName) && entry.PanelName == panelName)
            {
                return entry;
            }
        }

        return null;
    }

    private PanelEntry FindRuntimeByKey(string uiKey)
    {
        for (var i = 0; i < mRuntimeEntries.Count; i++)
        {
            var entry = mRuntimeEntries[i];
            if (entry != null && entry.UIKey == uiKey)
            {
                return entry;
            }
        }

        return null;
    }

    private PanelEntry FindRuntimeByPanel(string panelName)
    {
        for (var i = 0; i < mRuntimeEntries.Count; i++)
        {
            var entry = mRuntimeEntries[i];
            if (entry != null && entry.PanelName == panelName)
            {
                return entry;
            }
        }

        return null;
    }

    private PanelEntry FindRuntimeByKeyOrPanel(string uiKey, string panelName)
    {
        for (var i = 0; i < mRuntimeEntries.Count; i++)
        {
            var entry = mRuntimeEntries[i];
            if (entry == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(uiKey) && entry.UIKey == uiKey)
            {
                return entry;
            }

            if (!string.IsNullOrWhiteSpace(panelName) && entry.PanelName == panelName)
            {
                return entry;
            }
        }

        return null;
    }

    private static PanelEntry CreateMissingEntry(string uiKey)
    {
        return new PanelEntry
        {
            UIKey = uiKey,
            UIType = TableNineUIType.ChoiceOverlay,
            PanelName = uiKey,
            Level = UILevel.PopUI,
            OpenType = PanelOpenType.Single,
            FallbackStrategy = TableNineUIFallbackStrategy.AtomicChoiceList,
            BlocksGameplayInput = true,
            Notes = "Runtime missing entry generated as fallback."
        };
    }

    private static List<PanelEntry> CreateRecommendedDefaults()
    {
        return new List<PanelEntry>
        {
            new PanelEntry
            {
                UIKey = TableNineUIKeys.GameplayHud,
                UIType = TableNineUIType.Hud,
                PanelName = nameof(UIGameplayPanel),
                Level = UILevel.Common,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.None,
                BlocksGameplayInput = false,
                Notes = "Main HUD. Should always be backed by a formal prefab."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.PopupMessage,
                UIType = TableNineUIType.Popup,
                PanelName = nameof(UIPopupPanel),
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Multiple,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicPopup,
                BlocksGameplayInput = true,
                Notes = "Short blocking message popup."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.AttributeChoice,
                UIType = TableNineUIType.ChoiceOverlay,
                PanelName = nameof(UIChoiceOverlayPanel),
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicChoiceList,
                BlocksGameplayInput = true,
                Notes = "Attribute-up help card choice."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.RoomChoice,
                UIType = TableNineUIType.RoomChoice,
                PanelName = "UIRoomChoicePanel",
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicRoomChoice,
                BlocksGameplayInput = false,
                Notes = "Clear-ready room choice. Fallback only blocks its own buttons."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.HelpReward,
                UIType = TableNineUIType.RewardChoice,
                PanelName = "UIHelpRewardPanel",
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicChoiceList,
                BlocksGameplayInput = true,
                Notes = "Help card reward selection with skip."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.ChestReward,
                UIType = TableNineUIType.RewardChoice,
                PanelName = "UIChestRewardPanel",
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicChoiceList,
                BlocksGameplayInput = true,
                Notes = "Relic reward selection with skip."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.ShopMain,
                UIType = TableNineUIType.Shop,
                PanelName = "UIShopPanel",
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicShopList,
                BlocksGameplayInput = true,
                Notes = "Shop purchase/delete fallback list."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.TutorSkillChoice,
                UIType = TableNineUIType.ChoiceOverlay,
                PanelName = "UITutorSkillPanel",
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicChoiceList,
                BlocksGameplayInput = true,
                Notes = "Tutor skill selection."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.NextNodePrompt,
                UIType = TableNineUIType.StatusPrompt,
                PanelName = "UINextNodePanel",
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicStatusPrompt,
                BlocksGameplayInput = false,
                Notes = "Reserved status prompt. Not opened during help-reward→RoomChoosing; node advance uses ChooseRoomCommand / DescriptionPanel."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.DeleteHelpCardConfirm,
                UIType = TableNineUIType.Confirmation,
                PanelName = "UIDeleteHelpCardConfirmPanel",
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicChoiceList,
                BlocksGameplayInput = true,
                Notes = "Optional formal confirmation before deleting a help card."
            },
            new PanelEntry
            {
                UIKey = TableNineUIKeys.CardDetail,
                UIType = TableNineUIType.Detail,
                PanelName = "UICardDetailPanel",
                Level = UILevel.PopUI,
                OpenType = PanelOpenType.Single,
                FallbackStrategy = TableNineUIFallbackStrategy.AtomicPopup,
                BlocksGameplayInput = false,
                Notes = "Card detail tooltip/detail panel."
            }
        };
    }
}
