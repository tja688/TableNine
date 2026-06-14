#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public static class TableNineConfigEditorAutoSave
{
    private const string PrefsKey = "TableNine.ConfigEditor.AutoSaveEnabled";
    private const double SaveDelaySeconds = 0.35;

    private static bool _updateHooked;
    private static double _scheduledSaveTime = -1;

    public static bool IsEnabled
    {
        get => EditorPrefs.GetBool(PrefsKey, true);
        set => EditorPrefs.SetBool(PrefsKey, value);
    }

    public static ToolbarToggle CreateToolbarToggle(Action<bool> onChanged = null)
    {
        var toggle = new ToolbarToggle
        {
            text = "自动保存",
            value = IsEnabled
        };
        toggle.tooltip = "开启后编辑改动会自动写入磁盘；关闭后需手动点「保存」。";
        toggle.RegisterValueChangedCallback(evt =>
        {
            IsEnabled = evt.newValue;
            onChanged?.Invoke(evt.newValue);
            if (!evt.newValue)
            {
                FlushPending();
            }
        });
        return toggle;
    }

    private const string TrackerElementName = "TableNineConfigEditorAutoSaveTracker";

    public static void BindContentRoot(VisualElement root, SerializedObject serializedObject, UnityEngine.Object asset)
    {
        if (root == null || serializedObject == null || asset == null)
        {
            return;
        }

        // Track on a disposable child, not the persistent content root. Re-binding the root
        // after tab switches throws: "An element can track properties on only one serializedObject at a time".
        var tracker = new VisualElement { name = TrackerElementName };
        tracker.style.display = DisplayStyle.None;
        root.Add(tracker);

        tracker.TrackSerializedObjectValue(serializedObject, _ =>
        {
            PersistIfEnabled(serializedObject, asset);
        });
    }

    public static void PersistIfEnabled(SerializedObject serializedObject, UnityEngine.Object asset, bool immediateDisk = false)
    {
        if (!IsEnabled || serializedObject == null || asset == null)
        {
            return;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);

        if (immediateDisk)
        {
            AssetDatabase.SaveAssets();
            return;
        }

        ScheduleDiskSave();
    }

    public static void Persist(SerializedObject serializedObject, UnityEngine.Object asset, bool writeToDisk = true)
    {
        if (serializedObject == null || asset == null)
        {
            return;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);
        if (writeToDisk)
        {
            AssetDatabase.SaveAssets();
        }
    }

    public static void FlushPending()
    {
        if (_scheduledSaveTime < 0)
        {
            return;
        }

        _scheduledSaveTime = -1;
        AssetDatabase.SaveAssets();
        UnhookUpdate();
    }

    public static string GetFooterStatusLabel()
    {
        return IsEnabled ? "自动保存" : "手动保存";
    }

    private static void ScheduleDiskSave()
    {
        _scheduledSaveTime = EditorApplication.timeSinceStartup + SaveDelaySeconds;
        if (_updateHooked)
        {
            return;
        }

        _updateHooked = true;
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        if (_scheduledSaveTime < 0)
        {
            UnhookUpdate();
            return;
        }

        if (EditorApplication.timeSinceStartup < _scheduledSaveTime)
        {
            return;
        }

        _scheduledSaveTime = -1;
        if (IsEnabled)
        {
            AssetDatabase.SaveAssets();
        }

        UnhookUpdate();
    }

    private static void UnhookUpdate()
    {
        if (!_updateHooked)
        {
            return;
        }

        EditorApplication.update -= OnEditorUpdate;
        _updateHooked = false;
    }
}
#endif
