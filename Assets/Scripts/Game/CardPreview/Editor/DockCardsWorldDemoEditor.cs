using UnityEditor;
using UnityEngine;

/// <summary>
/// DockCardsWorldDemo 的 Scene 视图编辑器：将回收区可视化从 Game view 移至 Scene view。
/// </summary>
[CustomEditor(typeof(DockCardsWorldDemo))]
public sealed class DockCardsWorldDemoEditor : Editor
{
    private static readonly Color ZoneFill = new Color(0.82f, 0.9f, 0.78f, 0.12f);
    private static readonly Color ZoneHoverFill = new Color(0.92f, 0.98f, 0.88f, 0.18f);
    private static readonly Color ZoneBorder = new Color(0.72f, 0.86f, 0.7f, 0.68f);
    private static readonly Color ZoneHoverBorder = new Color(0.86f, 0.96f, 0.84f, 0.95f);
    private static readonly Color LabelColor = new Color(0.78f, 0.9f, 0.76f, 0.92f);

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

        var cameraProp = serializedObject.FindProperty("mCamera");
        var camera = cameraProp.objectReferenceValue as Camera;
        if (camera == null) camera = Camera.main;
        if (camera == null) return;

        var viewportRect = serializedObject.FindProperty("mRecycleZoneViewportRect").rectValue;
        var label = serializedObject.FindProperty("mRecycleZoneLabel").stringValue;
        var hint = serializedObject.FindProperty("mRecycleZoneHint").stringValue;

        DrawRecycleZone(camera, viewportRect, label, hint);
    }

    private void DrawRecycleZone(Camera cam, Rect viewportRect, string label, string hint)
    {
        var bl = ViewportToWorld(cam, viewportRect.xMin, viewportRect.yMin);
        var br = ViewportToWorld(cam, viewportRect.xMax, viewportRect.yMin);
        var tr = ViewportToWorld(cam, viewportRect.xMax, viewportRect.yMax);
        var tl = ViewportToWorld(cam, viewportRect.xMin, viewportRect.yMax);

        var mouseWorld = GetMouseWorldPosition(cam);
        var hovered = IsPointInQuad(mouseWorld, bl, br, tr, tl);

        // 半透明填充
        Handles.DrawSolidRectangleWithOutline(
            new[] { bl, br, tr, tl },
            hovered ? ZoneHoverFill : ZoneFill,
            Color.clear);

        // 边框线
        var borderColor = hovered ? ZoneHoverBorder : ZoneBorder;
        Handles.color = borderColor;
        Handles.DrawLine(bl, br);
        Handles.DrawLine(br, tr);
        Handles.DrawLine(tr, tl);
        Handles.DrawLine(tl, bl);

        // 标签
        var topCenter = (tl + tr) * 0.5f;
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = LabelColor }
        };

        Handles.Label(topCenter + Vector3.up * 0.15f, label, labelStyle);

        if (hovered)
        {
            var center = (bl + br + tr + tl) * 0.25f;
            var hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.68f, 0.82f, 0.66f, 0.9f) }
            };
            Handles.Label(center, hint, hintStyle);
        }

        Handles.color = Color.white;
    }

    private static Vector3 ViewportToWorld(Camera cam, float vpX, float vpY)
    {
        var depth = Mathf.Abs(cam.transform.position.z);
        return cam.ViewportToWorldPoint(new Vector3(vpX, vpY, depth));
    }

    private static Vector3 GetMouseWorldPosition(Camera cam)
    {
        var mp = Event.current.mousePosition;
        var guiScreenPos = new Vector2(mp.x, cam.pixelHeight - mp.y);
        var depth = Mathf.Abs(cam.transform.position.z);
        var world = cam.ScreenToWorldPoint(new Vector3(guiScreenPos.x, guiScreenPos.y, depth));
        world.z = 0f;
        return world;
    }

    private static bool IsPointInQuad(Vector3 point, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
    {
        var minX = Mathf.Min(bl.x, tl.x);
        var maxX = Mathf.Max(br.x, tr.x);
        var minY = Mathf.Min(bl.y, br.y);
        var maxY = Mathf.Max(tl.y, tr.y);
        return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
    }
}
