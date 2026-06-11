using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TableNineUI.Editor
{
    /// <summary>
    /// UI 面板管理窗口中的 DescriptionPanel 文案专属配置区。
    /// </summary>
    public static class DescriptionPanelConfigEditorSection
    {
        public const string NavKey = "__description_panel_texts__";

        private const string DefaultAssetPath = "Assets/ScriptableObjects/TableNineDescriptionPanelConfig.asset";

        public static TableNineDescriptionPanelConfig ResolveConfig(TableNineUIPanelRegistry registry)
        {
            if (registry != null && registry.DescriptionPanelConfig != null)
            {
                return registry.DescriptionPanelConfig;
            }

            var guids = AssetDatabase.FindAssets("t:TableNineDescriptionPanelConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TableNineDescriptionPanelConfig>(path);
            }

            return null;
        }

        public static TableNineDescriptionPanelConfig CreateDefaultAsset(TableNineUIPanelRegistry registry)
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }

            var config = ScriptableObject.CreateInstance<TableNineDescriptionPanelConfig>();
            config.ApplyRecommendedDefaults(replaceExistingText: true);
            AssetDatabase.CreateAsset(config, DefaultAssetPath);
            AssetDatabase.SaveAssets();

            if (registry != null)
            {
                Undo.RecordObject(registry, "Link Description Panel Config");
                registry.SetDescriptionPanelConfig(config);
                EditorUtility.SetDirty(registry);
            }

            return config;
        }

        public static void BuildDetailPane(
            VisualElement container,
            TableNineUIPanelRegistry registry,
            TableNineDescriptionPanelConfig config,
            Action refreshRequested)
        {
            container.Add(CreatePageHeader(
                "DescriptionPanel 描述文案",
                $"DescriptionPanel 单行展示，总字数不超过 {DescriptionPanelTextRules.MaxLength} 字（含空格；勿换行）。"));

            if (config == null)
            {
                container.Add(new HelpBox(
                    "尚未创建 DescriptionPanel 文案配置资产。点击下方按钮创建并写入精简默认文案。",
                    HelpBoxMessageType.Warning));

                var createBtn = new Button(() =>
                {
                    CreateDefaultAsset(registry);
                    refreshRequested?.Invoke();
                })
                {
                    text = "创建 DescriptionPanel 文案配置"
                };
                createBtn.style.height = 28;
                createBtn.style.marginBottom = 12;
                container.Add(createBtn);
                return;
            }

            var overLimit = config.CountOverLimitEntries();
            if (overLimit > 0)
            {
                container.Add(new HelpBox(
                    $"当前有 {overLimit} 条文案超过 {DescriptionPanelTextRules.MaxLength} 字，请精简或应用默认文案。",
                    HelpBoxMessageType.Error));
            }
            else
            {
                container.Add(new HelpBox(
                    $"共 {config.Entries.Count} 条文案，均符合 {DescriptionPanelTextRules.MaxLength} 字限制。",
                    HelpBoxMessageType.Info));
            }

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.marginBottom = 12;

            var mergeBtn = new Button(() =>
            {
                Undo.RecordObject(config, "Merge Description Panel Defaults");
                config.ApplyRecommendedDefaults(replaceExistingText: false);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                refreshRequested?.Invoke();
            })
            {
                text = "合并缺失条目"
            };
            mergeBtn.tooltip = "追加缺失键，不覆盖已有文案";
            toolbar.Add(mergeBtn);

            var resetBtn = new Button(() =>
            {
                if (!EditorUtility.DisplayDialog(
                        "应用精简默认文案",
                        "将把全部 DescriptionPanel 文案替换为代码内置的精简版本，是否继续？",
                        "继续",
                        "取消"))
                {
                    return;
                }

                Undo.RecordObject(config, "Reset Description Panel Texts");
                config.ApplyRecommendedDefaults(replaceExistingText: true);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                refreshRequested?.Invoke();
            })
            {
                text = "应用精简默认文案"
            };
            resetBtn.tooltip = "一次性清理：全部替换为不超过 40 字的默认文案";
            toolbar.Add(resetBtn);

            var pingBtn = new Button(() =>
            {
                EditorGUIUtility.PingObject(config);
                Selection.activeObject = config;
            })
            {
                text = "定位配置资产"
            };
            toolbar.Add(pingBtn);

            container.Add(toolbar);

            if (registry != null && registry.DescriptionPanelConfig != config)
            {
                container.Add(new HelpBox(
                    "当前配置资产尚未绑定到 UI Panel Registry。保存后会自动关联。",
                    HelpBoxMessageType.Warning));

                var linkBtn = new Button(() =>
                {
                    Undo.RecordObject(registry, "Link Description Panel Config");
                    registry.SetDescriptionPanelConfig(config);
                    EditorUtility.SetDirty(registry);
                    AssetDatabase.SaveAssets();
                    refreshRequested?.Invoke();
                })
                {
                    text = "绑定到 Registry"
                };
                linkBtn.style.height = 28;
                linkBtn.style.marginBottom = 12;
                container.Add(linkBtn);
            }

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;

            var serializedObject = new SerializedObject(config);
            var entriesProp = serializedObject.FindProperty("mEntries");
            if (entriesProp == null)
            {
                container.Add(new HelpBox("配置资产结构异常：找不到 mEntries。", HelpBoxMessageType.Error));
                return;
            }

            for (var i = 0; i < entriesProp.arraySize; i++)
            {
                var entryProp = entriesProp.GetArrayElementAtIndex(i);
                scroll.Add(BuildEntryCard(serializedObject, entryProp, i));
            }

            container.Add(scroll);

            serializedObject.ApplyModifiedProperties();
        }

        private static VisualElement BuildEntryCard(SerializedObject serializedObject, SerializedProperty entryProp, int index)
        {
            var keyProp = entryProp.FindPropertyRelative("Key");
            var labelProp = entryProp.FindPropertyRelative("Label");
            var textProp = entryProp.FindPropertyRelative("Text");
            var usageProp = entryProp.FindPropertyRelative("UsageNote");

            var card = new VisualElement();
            card.style.marginBottom = 10;
            card.style.backgroundColor = new Color(0.15f, 0.12f, 0.095f);
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;

            var title = new Label($"{labelProp.stringValue}  ·  {keyProp.stringValue}");
            title.style.fontSize = 13;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.95f, 0.89f, 0.79f);
            title.style.whiteSpace = WhiteSpace.Normal;
            card.Add(title);

            if (!string.IsNullOrEmpty(usageProp.stringValue))
            {
                var usage = new Label(usageProp.stringValue);
                usage.style.fontSize = 11;
                usage.style.color = new Color(0.79f, 0.73f, 0.67f);
                usage.style.marginTop = 4;
                usage.style.marginBottom = 8;
                usage.style.whiteSpace = WhiteSpace.Normal;
                card.Add(usage);
            }

            var counter = new Label();
            counter.style.fontSize = 11;
            counter.style.marginBottom = 4;
            card.Add(counter);

            var warningBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            warningBox.style.display = DisplayStyle.None;
            warningBox.style.marginBottom = 6;
            card.Add(warningBox);

            var textField = new TextField { multiline = false, value = textProp.stringValue };
            textField.style.minHeight = 28;
            textField.style.whiteSpace = WhiteSpace.Normal;

            void RefreshValidation(string value)
            {
                var length = DescriptionPanelTextRules.CountLength(value);
                var overLimit = length > DescriptionPanelTextRules.MaxLength;
                var hasLineBreak = DescriptionPanelTextRules.ContainsLineBreak(value);

                counter.text = $"{length} / {DescriptionPanelTextRules.MaxLength} 字";
                counter.style.color = overLimit
                    ? new Color(0.95f, 0.45f, 0.35f)
                    : new Color(0.83f, 0.78f, 0.72f);

                if (overLimit || hasLineBreak)
                {
                    warningBox.style.display = DisplayStyle.Flex;
                    warningBox.text = overLimit
                        ? $"超出 {DescriptionPanelTextRules.MaxLength} 字限制，DescriptionPanel 会溢出。"
                        : "检测到换行符。换行时空格也计字数，请尽量保持单行。";
                }
                else
                {
                    warningBox.style.display = DisplayStyle.None;
                }
            }

            RefreshValidation(textField.value);

            textField.RegisterValueChangedCallback(evt =>
            {
                textProp.stringValue = evt.newValue ?? string.Empty;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                RefreshValidation(evt.newValue);
            });

            card.Add(textField);
            return card;
        }

        private static VisualElement CreatePageHeader(string title, string description)
        {
            var block = new VisualElement();
            block.style.marginBottom = 14;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 24;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new Color(0.95f, 0.90f, 0.80f);
            block.Add(titleLabel);

            if (!string.IsNullOrEmpty(description))
            {
                var desc = new Label(description);
                desc.style.fontSize = 12;
                desc.style.color = new Color(0.80f, 0.74f, 0.67f);
                desc.style.marginTop = 6;
                desc.style.marginBottom = 14;
                desc.style.whiteSpace = WhiteSpace.Normal;
                block.Add(desc);
            }

            return block;
        }
    }
}
