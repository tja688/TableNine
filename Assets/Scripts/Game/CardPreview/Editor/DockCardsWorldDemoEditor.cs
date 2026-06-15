using UnityEditor;
using UnityEngine;

/// <summary>
/// DockCardsWorldDemo 的 Scene 视图编辑器：像 2D 盒碰撞器一样调整底板收缩 / 膨胀区域。
/// </summary>
[CustomEditor(typeof(DockCardsWorldDemo))]
public sealed class DockCardsWorldDemoEditor : Editor
{
    private const float MinRectSize = 0.2f;
    private static readonly Color CollapsedFill = new Color(0.82f, 0.9f, 0.98f, 0.12f);
    private static readonly Color CollapsedBorder = new Color(0.72f, 0.86f, 0.98f, 0.92f);
    private static readonly Color ExpandedFill = new Color(0.98f, 0.93f, 0.8f, 0.12f);
    private static readonly Color ExpandedBorder = new Color(0.98f, 0.94f, 0.82f, 0.92f);
    private static readonly Color LabelColor = new Color(0.98f, 0.98f, 0.98f, 0.96f);

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        var dock = (DockCardsWorldDemo)target;
        if (dock == null) return;

        serializedObject.Update();

        var cameraProp = serializedObject.FindProperty("mCamera");
        var camera = cameraProp.objectReferenceValue as Camera;
        if (camera == null) camera = Camera.main;
        if (camera == null) return;

        var viewportY = serializedObject.FindProperty("mViewportY").floatValue;
        var anchor = ViewportToWorld(camera, 0.5f, viewportY);

        var collapsedRectProp = serializedObject.FindProperty("mDockBackgroundCollapsedRectLocal");
        var expandedRectProp = serializedObject.FindProperty("mDockBackgroundExpandedRectLocal");

        var changed = false;
        changed |= DrawEditableRect(anchor, collapsedRectProp, "Dock BG Collapsed", CollapsedFill, CollapsedBorder);
        changed |= DrawEditableRect(anchor, expandedRectProp, "Dock BG Expanded / play-out", ExpandedFill, ExpandedBorder);

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
        else
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static bool DrawEditableRect(
        Vector3 anchor,
        SerializedProperty rectProperty,
        string label,
        Color fillColor,
        Color borderColor)
    {
        var localRect = SanitizeRect(rectProperty.rectValue);
        var worldRect = ToWorldRect(anchor, localRect);

        DrawRect(worldRect, label, fillColor, borderColor);

        EditorGUI.BeginChangeCheck();
        var editedWorldRect = EditRectHandles(worldRect, borderColor);
        if (!EditorGUI.EndChangeCheck())
        {
            return false;
        }

        rectProperty.rectValue = SanitizeRect(ToLocalRect(anchor, editedWorldRect));
        return true;
    }

    private static void DrawRect(Rect worldRect, string label, Color fillColor, Color borderColor)
    {
        var bl = new Vector3(worldRect.xMin, worldRect.yMin, 0f);
        var br = new Vector3(worldRect.xMax, worldRect.yMin, 0f);
        var tr = new Vector3(worldRect.xMax, worldRect.yMax, 0f);
        var tl = new Vector3(worldRect.xMin, worldRect.yMax, 0f);

        Handles.DrawSolidRectangleWithOutline(new[] { bl, br, tr, tl }, fillColor, borderColor);

        var topCenter = (tl + tr) * 0.5f;
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = LabelColor }
        };

        Handles.Label(topCenter + Vector3.up * 0.12f, label, labelStyle);
    }

    private static Vector3 ViewportToWorld(Camera cam, float vpX, float vpY)
    {
        var depth = Mathf.Abs(cam.transform.position.z);
        return cam.ViewportToWorldPoint(new Vector3(vpX, vpY, depth));
    }

    private static Rect EditRectHandles(Rect rect, Color color)
    {
        using (new Handles.DrawingScope(color))
        {
            var center = rect.center;
            var handleSize = HandleUtility.GetHandleSize(new Vector3(center.x, center.y, 0f)) * 0.08f;

            var movedCenter = Handles.FreeMoveHandle(
                new Vector3(center.x, center.y, 0f),
                handleSize,
                Vector3.zero,
                Handles.RectangleHandleCap);
            var centerDelta = (Vector2)movedCenter - center;
            rect.position += centerDelta;

            var left = Handles.Slider(
                new Vector3(rect.xMin, rect.center.y, 0f),
                Vector3.left,
                handleSize,
                Handles.RectangleHandleCap,
                0f);
            rect.xMin = Mathf.Min(left.x, rect.xMax - MinRectSize);

            var right = Handles.Slider(
                new Vector3(rect.xMax, rect.center.y, 0f),
                Vector3.right,
                handleSize,
                Handles.RectangleHandleCap,
                0f);
            rect.xMax = Mathf.Max(right.x, rect.xMin + MinRectSize);

            var bottom = Handles.Slider(
                new Vector3(rect.center.x, rect.yMin, 0f),
                Vector3.down,
                handleSize,
                Handles.RectangleHandleCap,
                0f);
            rect.yMin = Mathf.Min(bottom.y, rect.yMax - MinRectSize);

            var top = Handles.Slider(
                new Vector3(rect.center.x, rect.yMax, 0f),
                Vector3.up,
                handleSize,
                Handles.RectangleHandleCap,
                0f);
            rect.yMax = Mathf.Max(top.y, rect.yMin + MinRectSize);
        }

        return rect;
    }

    private static Rect ToWorldRect(Vector3 anchor, Rect localRect)
    {
        return Rect.MinMaxRect(
            anchor.x + localRect.xMin,
            anchor.y + localRect.yMin,
            anchor.x + localRect.xMax,
            anchor.y + localRect.yMax);
    }

    private static Rect ToLocalRect(Vector3 anchor, Rect worldRect)
    {
        return Rect.MinMaxRect(
            worldRect.xMin - anchor.x,
            worldRect.yMin - anchor.y,
            worldRect.xMax - anchor.x,
            worldRect.yMax - anchor.y);
    }

    private static Rect SanitizeRect(Rect rect)
    {
        if (rect.width < MinRectSize)
        {
            rect.width = MinRectSize;
        }

        if (rect.height < MinRectSize)
        {
            rect.height = MinRectSize;
        }

        return rect;
    }
}
