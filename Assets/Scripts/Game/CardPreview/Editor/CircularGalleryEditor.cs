using UnityEditor;
using UnityEngine;

/// <summary>
/// CircularGalleryWorldDemo 的自定义 Scene 视图编辑器。
/// 绘制贝塞尔曲线 + 三个控制点手柄 + 判定区矩形。
/// </summary>
[CustomEditor(typeof(CircularGalleryWorldDemo))]
public sealed class CircularGalleryEditor : Editor
{
    private SerializedProperty mPropStart;
    private SerializedProperty mPropMid;
    private SerializedProperty mPropEnd;
    private SerializedProperty mPropJudgmentCenter;
    private SerializedProperty mPropJudgmentSize;
    private SerializedProperty mPropCardCount;

    private bool mShowBezier = true;
    private bool mShowJudgment = true;

    private static readonly Color CurveColor = new Color(0.2f, 0.7f, 1f, 0.9f);
    private static readonly Color GuideColor = new Color(1f, 1f, 1f, 0.25f);
    private static readonly Color JudgmentFill = new Color(1f, 0.85f, 0.3f, 0.08f);
    private static readonly Color JudgmentBorder = new Color(1f, 0.85f, 0.3f, 0.6f);

    private const int CurveSegments = 3;
    private const int PointsPerSegment = 24;
    private const float HandleSizeFactor = 0.04f;

    private void OnEnable()
    {
        mPropStart = serializedObject.FindProperty("mControlPointStart");
        mPropMid = serializedObject.FindProperty("mControlPointMid");
        mPropEnd = serializedObject.FindProperty("mControlPointEnd");
        mPropJudgmentCenter = serializedObject.FindProperty("mJudgmentCenter");
        mPropJudgmentSize = serializedObject.FindProperty("mJudgmentSize");
        mPropCardCount = serializedObject.FindProperty("mCardCount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        mShowBezier = EditorGUILayout.Foldout(mShowBezier, "Bezier Arc Control Points", true);
        if (mShowBezier)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(mPropStart, new GUIContent("Start (P0)"));
            EditorGUILayout.PropertyField(mPropMid, new GUIContent("Mid (P1)"));
            EditorGUILayout.PropertyField(mPropEnd, new GUIContent("End (P2)"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        mShowJudgment = EditorGUILayout.Foldout(mShowJudgment, "Judgment Area", true);
        if (mShowJudgment)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(mPropJudgmentCenter, new GUIContent("Center"));
            EditorGUILayout.PropertyField(mPropJudgmentSize, new GUIContent("Size"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(mPropCardCount);

        if (serializedObject.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        var gallery = (CircularGalleryWorldDemo)target;
        var tr = gallery.transform;

        DrawBezierHandles(gallery, tr);
        DrawJudgmentAreaHandles(gallery, tr);
    }

    // ════════════════════════════════════════════════════
    //  BEZIER HANDLES
    // ════════════════════════════════════════════════════

    private void DrawBezierHandles(CircularGalleryWorldDemo gallery, Transform tr)
    {
        var p0w = tr.TransformPoint(gallery.mControlPointStart);
        var p1w = tr.TransformPoint(gallery.mControlPointMid);
        var p2w = tr.TransformPoint(gallery.mControlPointEnd);

        var handleSize = HandleUtility.GetHandleSize(p1w) * HandleSizeFactor;

        // ── 辅助线 ──
        Handles.color = GuideColor;
        Handles.DrawLine(p0w, p1w);
        Handles.DrawLine(p1w, p2w);

        // ── 主曲线（二次→三次贝塞尔近似）──
        Handles.color = CurveColor;
        for (var s = 0; s < CurveSegments; s++)
        {
            var tStart = (float)s / CurveSegments;
            var tEnd = (float)(s + 1) / CurveSegments;
            var dt = tEnd - tStart;
            var segStart = BezierArc.Eval(tStart, p0w, p1w, p2w);
            var segEnd = BezierArc.Eval(tEnd, p0w, p1w, p2w);
            // 三次贝塞尔控制点：C1 = P_start + tangent_start * dt/3
            //                   C2 = P_end   - tangent_end   * dt/3
            var scale = dt / 3f;
            var c1 = segStart + BezierArc.Tangent(tStart, p0w, p1w, p2w) * scale;
            var c2 = segEnd - BezierArc.Tangent(tEnd, p0w, p1w, p2w) * scale;
            Handles.DrawBezier(segStart, segEnd, c1, c2, CurveColor, null, 3f);
        }

        // ── 卡牌位置标记 ──
        var cardCount = Mathf.Max(gallery.mCardCount, 1);
        Handles.color = new Color(0.2f, 0.7f, 1f, 0.5f);
        for (var i = 0; i < cardCount; i++)
        {
            var t = cardCount > 1 ? (float)i / (cardCount - 1) : 0.5f;
            var pos = BezierArc.Eval(t, p0w, p1w, p2w);
            var tan = BezierArc.Tangent(t, p0w, p1w, p2w);
            var normal = new Vector3(-tan.y, tan.x, 0f).normalized;

            Handles.DrawLine(pos - normal * 0.25f, pos + normal * 0.25f);
        }

        // ── P0 手柄 (绿) ──
        EditorGUI.BeginChangeCheck();
        var newP0 = Handles.PositionHandle(p0w, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gallery, "Move Bezier Start");
            gallery.mControlPointStart = tr.InverseTransformPoint(newP0);
            EditorUtility.SetDirty(gallery);
        }
        Handles.color = new Color(0.3f, 0.9f, 0.3f, 0.9f);
        Handles.SphereHandleCap(0, p0w, Quaternion.identity, handleSize * 4f, EventType.Repaint);

        // ── P1 手柄 (黄) ──
        EditorGUI.BeginChangeCheck();
        var newP1 = Handles.PositionHandle(p1w, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gallery, "Move Bezier Mid");
            gallery.mControlPointMid = tr.InverseTransformPoint(newP1);
            EditorUtility.SetDirty(gallery);
        }
        Handles.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Handles.SphereHandleCap(0, p1w, Quaternion.identity, handleSize * 4f, EventType.Repaint);

        // ── P2 手柄 (红) ──
        EditorGUI.BeginChangeCheck();
        var newP2 = Handles.PositionHandle(p2w, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gallery, "Move Bezier End");
            gallery.mControlPointEnd = tr.InverseTransformPoint(newP2);
            EditorUtility.SetDirty(gallery);
        }
        Handles.color = new Color(0.9f, 0.3f, 0.3f, 0.9f);
        Handles.SphereHandleCap(0, p2w, Quaternion.identity, handleSize * 4f, EventType.Repaint);

        // ── 标签 ──
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };
        Handles.Label(p0w + Vector3.up * 0.3f, "P0 Start", labelStyle);
        Handles.Label(p1w + Vector3.up * 0.3f, "P1 Mid", labelStyle);
        Handles.Label(p2w + Vector3.up * 0.3f, "P2 End", labelStyle);
    }

    // ════════════════════════════════════════════════════
    //  JUDGMENT AREA HANDLES
    // ════════════════════════════════════════════════════

    private void DrawJudgmentAreaHandles(CircularGalleryWorldDemo gallery, Transform tr)
    {
        var center = gallery.mJudgmentCenter;
        var size = gallery.mJudgmentSize;
        var halfW = size.x * 0.5f;
        var halfH = size.y * 0.5f;

        // 四角世界坐标
        var bl = tr.TransformPoint(new Vector3(center.x - halfW, center.y - halfH, 0f));
        var br = tr.TransformPoint(new Vector3(center.x + halfW, center.y - halfH, 0f));
        var tl = tr.TransformPoint(new Vector3(center.x - halfW, center.y + halfH, 0f));
        var trCorner = tr.TransformPoint(new Vector3(center.x + halfW, center.y + halfH, 0f));

        // 填充
        Handles.color = JudgmentFill;
        Handles.DrawAAConvexPolygon(bl, br, trCorner, tl);

        // 边框
        Handles.color = JudgmentBorder;
        Handles.DrawLine(bl, br);
        Handles.DrawLine(br, trCorner);
        Handles.DrawLine(trCorner, tl);
        Handles.DrawLine(tl, bl);

        // ── 中心手柄 ──
        var centerWorld = tr.TransformPoint(new Vector3(center.x, center.y, 0f));
        EditorGUI.BeginChangeCheck();
        var newCenter = Handles.PositionHandle(centerWorld, tr.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gallery, "Move Judgment Center");
            var localCenter = tr.InverseTransformPoint(newCenter);
            gallery.mJudgmentCenter = new Vector2(localCenter.x, localCenter.y);
            EditorUtility.SetDirty(gallery);
        }

        // ── 右上角尺寸手柄 ──
        EditorGUI.BeginChangeCheck();
        var newTR = Handles.PositionHandle(trCorner, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gallery, "Resize Judgment Area");
            var localTR = tr.InverseTransformPoint(newTR);
            var dx = localTR.x - center.x;
            var dy = localTR.y - center.y;
            gallery.mJudgmentSize = new Vector2(
                Mathf.Max(2f, Mathf.Abs(dx) * 2f),
                Mathf.Max(2f, Mathf.Abs(dy) * 2f));
            EditorUtility.SetDirty(gallery);
        }

        // ── 左下角尺寸手柄 ──
        EditorGUI.BeginChangeCheck();
        var newBL = Handles.PositionHandle(bl, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gallery, "Resize Judgment Area");
            var localBL = tr.InverseTransformPoint(newBL);
            var dx = center.x - localBL.x;
            var dy = center.y - localBL.y;
            gallery.mJudgmentSize = new Vector2(
                Mathf.Max(2f, Mathf.Abs(dx) * 2f),
                Mathf.Max(2f, Mathf.Abs(dy) * 2f));
            EditorUtility.SetDirty(gallery);
        }

        // 标签
        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = JudgmentBorder }
        };
        Handles.Label(centerWorld + Vector3.up * (halfH + 0.2f),
            $"Judgment Area ({size.x:F1} x {size.y:F1})", labelStyle);
    }
}
