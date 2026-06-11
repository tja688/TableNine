using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

public class TMPMissingGlyphChecker : EditorWindow
{
    public TMP_FontAsset fontAsset;
    public TextAsset textFile;
    public bool searchFallbacks = true;

    [MenuItem("Tools/Text/Check TMP Missing Glyphs")]
    public static void Open()
    {
        GetWindow<TMPMissingGlyphChecker>("TMP Missing Glyphs");
    }

    private void OnGUI()
    {
        fontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "TMP Font Asset",
            fontAsset,
            typeof(TMP_FontAsset),
            false
        );

        textFile = (TextAsset)EditorGUILayout.ObjectField(
            "All Game Text",
            textFile,
            typeof(TextAsset),
            false
        );

        searchFallbacks = EditorGUILayout.Toggle("Search Fallbacks", searchFallbacks);

        if (GUILayout.Button("Check Missing Glyphs"))
        {
            Check();
        }
    }

    private void Check()
    {
        if (fontAsset == null || textFile == null)
        {
            Debug.LogWarning("请先指定 TMP Font Asset 和文本文件。");
            return;
        }

        var missing = new SortedSet<char>();

        foreach (char c in textFile.text)
        {
            if (char.IsWhiteSpace(c))
                continue;

            if (!fontAsset.HasCharacter(c, searchFallbacks))
                missing.Add(c);
        }

        if (missing.Count == 0)
        {
            Debug.Log("没有发现缺字。");
            return;
        }

        var builder = new StringBuilder();
        foreach (char c in missing)
        {
            builder.Append(c);
        }

        Debug.LogWarning($"缺字数量：{missing.Count}\n{builder}");
    }
}