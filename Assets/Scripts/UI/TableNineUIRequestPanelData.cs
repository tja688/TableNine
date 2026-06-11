using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public sealed class TableNineUIRequestPanelData : UIPanelData
{
    public string Key;
    public string Title;
    public string Message;
    public TableNineUIType UIType;
    public TableNineUIFallbackStrategy FallbackStrategy;
    public bool BlocksGameplayInput;
    public bool CloseOnChoice = true;
    public string EmptyText = "暂无可选项。";
    public string CloseLabel;
    public Action<IController> CloseAction;
    public readonly List<TableNineUIChoiceData> Choices = new List<TableNineUIChoiceData>();
    public readonly List<TableNineUIChoiceData> SecondaryChoices = new List<TableNineUIChoiceData>();

    // Fallback 组件化预制件引用（由 TableNineUIRuntime 从 Registry 注入）
    public GameObject FallbackButtonPrefab;
    public GameObject FallbackTextPrefab;
    public GameObject FallbackIconPrefab;
    public GameObject FallbackPanelPrefab;
    public GameObject FallbackScrollViewPrefab;
}

public sealed class TableNineUIChoiceData
{
    public string Id;
    public string Label;
    public string Description;
    public string MetaText;
    public bool IsEnabled = true;
    public bool CloseAfterClick = true;
    public Action<IController> Action;

    public static TableNineUIChoiceData Command(
        string id,
        string label,
        string description,
        string metaText,
        Action<IController> action)
    {
        return new TableNineUIChoiceData
        {
            Id = id,
            Label = label,
            Description = description,
            MetaText = metaText,
            Action = action
        };
    }
}
