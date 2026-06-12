#if UNITY_EDITOR
using System.Collections.Generic;

public static class TableNineUIRuntimeTestRecorder
{
    public static readonly List<string> OpenedKeys = new List<string>();
    public static readonly List<string> ClosedKeys = new List<string>();

    public static bool SkipActualPanelOpen { get; set; }

    public static void RecordOpen(string key)
    {
        OpenedKeys.Add(key);
    }

    public static void RecordClose(string key)
    {
        ClosedKeys.Add(key);
    }

    public static void Reset()
    {
        OpenedKeys.Clear();
        ClosedKeys.Clear();
        SkipActualPanelOpen = false;
    }
}
#endif
