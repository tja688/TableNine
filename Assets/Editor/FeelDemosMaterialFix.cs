using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Fixes Feel Demo materials that reference URP shaders in a Built-in Render Pipeline project.
/// 
/// Root Cause: Feel (MoreMountains) ships its demo materials pre-configured for URP.
/// In a Built-in pipeline project without URP installed, these materials appear pink/broken.
/// This script remaps URP shader references to their Built-in equivalents and migrates
/// common material properties (color, texture, etc.) from URP naming to Standard naming.
/// </summary>
public class FeelDemosMaterialFix : EditorWindow
{
    // --- URP Shader GUIDs (from uninstalled URP package or URP-only custom shaders) ---
    
    // URP package shaders (GUIDs from the URP package, not present in project)
    private const string URP_LIT_GUID              = "933532a4fcc9baf4fa0491de14d08ed7";
    private const string URP_PARTICLES_UNLIT_GUID   = "0406db5a14f94604a8c57ccfbc9f3b46";
    private const string URP_SIMPLELIT_GUID         = "c15834894887d4c4b935cfe9df6f1c89";
    private const string URP_PARTICLES_LIT_GUID     = "13c02b14c4d048fa9653293d54f6e0e1";
    
    // Custom URP shaders (present in project but require URP to compile)
    private const string MM_ADVANCEDTOON_URP_GUID   = "d4b1f8e8d330f2b49989b5d7a3a6ade5";
    private const string MM_WORLDSPACE_URP_GUID     = "3961bf3f2f10811478fb5d4f936b3c5c";
    private const string MM_2DREFLECTION_URP_GUID   = "d53cd4050c2c9b842864f89ad97c8a83";

    // --- Built-in Shader Replacements ---
    private static readonly ShaderInfo BuiltinStandard   = new ShaderInfo("Standard", 46);
    private static readonly ShaderInfo BuiltinPartUnlit  = new ShaderInfo("Particles/Standard Unlit", 48);

    // --- Custom Built-in Shader Replacements (GUIDs from .meta files) ---
    private static readonly ShaderInfo MM_AdvancedToon   = new ShaderInfo("MoreMountains/MMAdvancedToon",
        "82c42a0b06f4f684db1ee2c535fb985c");
    private static readonly ShaderInfo MM_Worldspace     = new ShaderInfo("MoreMountains/MMWorldspace",
        "93b39336c7407ba4cb17a6d77dfa9605");
    private static readonly ShaderInfo MM_2DReflection   = new ShaderInfo("MoreMountains/MM2DReflection",
        "ad7325dd3fb876645bde2dd9d0691993");

    // --- URP Property Name -> Standard Property Name ---
    private static readonly Dictionary<string, string> PropertyRemap = new Dictionary<string, string>
    {
        // Color
        { "_BaseColor", "_Color" },
        // Main Texture
        { "_BaseMap", "_MainTex" },
        // Metallic / Smoothness
        { "_MetallicGlossMap", "_MetallicGlossMap" },
        { "_Metallic", "_Metallic" },
        { "_Smoothness", "_GlossMapScale" },
        { "_SmoothnessTextureChannel", "_SmoothnessTextureChannel" },
        // Normal
        { "_BumpMap", "_BumpMap" },
        { "_BumpScale", "_BumpScale" },
        // Emission
        { "_EmissionMap", "_EmissionMap" },
        { "_EmissionColor", "_EmissionColor" },
        // Occlusion
        { "_OcclusionMap", "_OcclusionMap" },
        { "_OcclusionStrength", "_OcclusionStrength" },
        // Specular (for Standard Specular variant)
        { "_SpecColor", "_SpecColor" },
        { "_SpecGlossMap", "_SpecGlossMap" },
        // Parallax
        { "_ParallaxMap", "_ParallaxMap" },
        { "_Parallax", "_Parallax" },
        // Detail
        { "_DetailMask", "_DetailMask" },
        { "_DetailAlbedoMap", "_DetailAlbedoMap" },
        { "_DetailNormalMap", "_DetailNormalMap" },
        { "_DetailNormalMapScale", "_DetailNormalMapScale" },
    };

    // URP-only shader keywords that should be cleaned up on Standard shader
    private static readonly string[] UrpOnlyKeywords = new[]
    {
        "_SPECULAR_SETUP",
        "_RECEIVE_SHADOWS_OFF",
        "_SURFACE_TYPE_TRANSPARENT",
        "_ALPHATEST_ON",
        "_ALPHAPREMULTIPLY_ON",
        "_ALPHAMODULATE_ON",
        "_FORWARD_PLUS",
    };

    private Vector2 _scrollPos;
    private string _logOutput = "Ready. Click 'Fix Materials' to scan and remap.";
    private int _fixedCount;
    private int _scannedCount;

    [MenuItem("Tools/Feel Demos/Fix Materials for Built-in Pipeline")]
    public static void ShowWindow()
    {
        var window = GetWindow<FeelDemosMaterialFix>("Fix Feel Materials");
        window.minSize = new Vector2(420, 360);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fix Feel Demo Materials for Built-in Render Pipeline",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Root Cause: Feel ships demo materials configured for URP shaders.\n" +
            "This tool remaps them to Built-in equivalents (Standard, etc.) and migrates\n" +
            "common properties like color, main texture, metallic, normal map, emission.",
            MessageType.Warning);
        EditorGUILayout.Space();

        if (GUILayout.Button("Fix Materials (Scan & Remap)", GUILayout.Height(36)))
        {
            FixAllMaterials();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Log:", EditorStyles.boldLabel);
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
        EditorGUILayout.TextArea(_logOutput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Main entry: scans all .mat files under Assets/Feel/FeelDemos and remaps URP shaders.
    /// </summary>
    private void FixAllMaterials()
    {
        _logOutput = "";
        _fixedCount = 0;
        _scannedCount = 0;

        // Find all .mat files under FeelDemos
        var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Feel/FeelDemos" });
        Log($"Found {matGuids.Length} materials under Assets/Feel/FeelDemos.\n");

        foreach (var guid in matGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            _scannedCount++;

            try
            {
                if (FixMaterial(assetPath))
                    _fixedCount++;
            }
            catch (System.Exception e)
            {
                Log($"[ERROR] {assetPath}: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log($"\n===== Done =====");
        Log($"Scanned: {_scannedCount}  |  Fixed: {_fixedCount}");
        Debug.Log($"[FeelDemosMaterialFix] Scanned {_scannedCount} materials, fixed {_fixedCount}.");
    }

    /// <summary>
    /// Returns true if the material was modified.
    /// </summary>
    private bool FixMaterial(string assetPath)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null || mat.shader == null)
            return false;

        // Read the raw YAML to get the actual shader GUID
        // (mat.shader might be "Hidden/InternalErrorShader" if the original shader is missing)
        var shaderGuid = GetShaderGuidFromYaml(assetPath);
        if (string.IsNullOrEmpty(shaderGuid))
            return false;

        ShaderInfo? replacement = GetReplacement(shaderGuid);
        if (replacement == null)
            return false; // Not a URP shader reference

        Shader newShader = Shader.Find(replacement.Value.name);
        if (newShader == null)
        {
            Log($"[WARN] Shader.Find(\"{replacement.Value.name}\") returned null for {Path.GetFileName(assetPath)}");
            return false;
        }

        // If it's already on the correct shader, skip
        if (mat.shader == newShader && shaderGuid != URP_LIT_GUID && shaderGuid != URP_PARTICLES_UNLIT_GUID
            && shaderGuid != URP_SIMPLELIT_GUID && shaderGuid != URP_PARTICLES_LIT_GUID)
            return false;

        // --- Migrate properties BEFORE changing shader ---
        var oldShader = mat.shader;
        bool isTransparent = false;

        // Collect old properties (only those actually set on the material)
        var oldProps = CollectMaterialProperties(mat);

        // Transfer common properties (same name in both URP and Standard)
        foreach (var kvp in PropertyRemap)
        {
            if (!oldProps.ContainsKey(kvp.Key)) continue;

            var propData = oldProps[kvp.Key];
            TransferProperty(mat, propData, kvp.Value);
        }

        // Handle URP transparency flag
        if (oldProps.ContainsKey("_Surface"))
        {
            // _Surface: 0 = Opaque, 1 = Transparent in URP
            isTransparent = oldProps["_Surface"].floatValue > 0.5f;
        }

        // --- Swap shader ---
        mat.shader = newShader;

        // --- Apply transparency mode ---
        if (isTransparent)
        {
            // Standard shader mode 3 = Transparent
            if (mat.HasProperty("_Mode"))
                mat.SetFloat("_Mode", 3f);
            if (mat.HasProperty("_SrcBlend"))
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = 3000;
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.SetShaderPassEnabled("DepthOnly", false);
        }
        else
        {
            if (mat.HasProperty("_Mode"))
                mat.SetFloat("_Mode", 0f); // Opaque
            if (mat.HasProperty("_SrcBlend"))
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (mat.HasProperty("_DstBlend"))
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 1f);
        }

        // --- Clean up URP-specific keywords ---
        foreach (var kw in UrpOnlyKeywords)
        {
            if (mat.IsKeywordEnabled(kw))
                mat.DisableKeyword(kw);
        }

        EditorUtility.SetDirty(mat);
        Log($"[FIXED] {Path.GetFileName(assetPath)}: {oldShader.name} -> {newShader.name}");
        return true;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private struct ShaderInfo
    {
        public string name;
        public string guid;   // null for built-in Unity shaders (use fileID instead)
        public int fileID;

        public ShaderInfo(string name, int fileID)
        {
            this.name = name;
            this.guid = null;
            this.fileID = fileID;
        }

        public ShaderInfo(string name, string guid)
        {
            this.name = name;
            this.guid = guid;
            this.fileID = 48;
        }
    }

    private struct PropData
    {
        public ShaderUtil.ShaderPropertyType type;
        public Color colorValue;
        public Vector4 vectorValue;
        public float floatValue;
        public Texture textureValue;
        public Vector2 textureScale;
        public Vector2 textureOffset;
    }

    private ShaderInfo? GetReplacement(string shaderGuid)
    {
        switch (shaderGuid)
        {
            case URP_LIT_GUID:
            case URP_SIMPLELIT_GUID:
            case URP_PARTICLES_LIT_GUID:
                return BuiltinStandard;

            case URP_PARTICLES_UNLIT_GUID:
                return BuiltinPartUnlit;

            case MM_ADVANCEDTOON_URP_GUID:
                return MM_AdvancedToon;

            case MM_WORLDSPACE_URP_GUID:
                return MM_Worldspace;

            case MM_2DREFLECTION_URP_GUID:
                return MM_2DReflection;

            default:
                return null;
        }
    }

    /// <summary>
    /// Reads the .mat YAML file directly to extract the shader GUID.
    /// This works even when the shader is missing (pink material).
    /// </summary>
    private static string GetShaderGuidFromYaml(string assetPath)
    {
        var fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath)) return null;

        var lines = File.ReadAllLines(fullPath);
        // Look for "m_Shader: {fileID: ..., guid: ..., type: ...}"
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("m_Shader:"))
            {
                var line = lines[i];
                var guidIdx = line.IndexOf("guid: ", System.StringComparison.Ordinal);
                if (guidIdx >= 0)
                {
                    var start = guidIdx + 6;
                    var end = line.IndexOf(',', start);
                    if (end < 0) end = line.IndexOf('}', start);
                    if (end > start)
                        return line.Substring(start, end - start).Trim();
                }
                // Built-in shader (fileID: 46, no guid) - not a URP reference
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Collects all properties currently set on the material.
    /// </summary>
    private static Dictionary<string, PropData> CollectMaterialProperties(Material mat)
    {
        var dict = new Dictionary<string, PropData>();
        var shader = mat.shader;
        if (shader == null) return dict;

        int count = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < count; i++)
        {
            string name = ShaderUtil.GetPropertyName(shader, i);
            var type = ShaderUtil.GetPropertyType(shader, i);

            try
            {
                var data = new PropData { type = type };
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        if (mat.HasProperty(name))
                            data.colorValue = mat.GetColor(name);
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        if (mat.HasProperty(name))
                            data.vectorValue = mat.GetVector(name);
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        if (mat.HasProperty(name))
                            data.floatValue = mat.GetFloat(name);
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        if (mat.HasProperty(name))
                        {
                            data.textureValue = mat.GetTexture(name);
                            data.textureScale = mat.GetTextureScale(name);
                            data.textureOffset = mat.GetTextureOffset(name);
                        }
                        break;
                }
                dict[name] = data;
            }
            catch
            {
                // Property might not be set, skip
            }
        }
        return dict;
    }

    /// <summary>
    /// Transfers a property value to the material under a new name.
    /// </summary>
    private static void TransferProperty(Material mat, PropData data, string targetName)
    {
        if (!mat.HasProperty(targetName)) return;

        try
        {
            switch (data.type)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                    mat.SetColor(targetName, data.colorValue);
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    mat.SetVector(targetName, data.vectorValue);
                    break;
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    mat.SetFloat(targetName, data.floatValue);
                    break;
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    mat.SetTexture(targetName, data.textureValue);
                    mat.SetTextureScale(targetName, data.textureScale);
                    mat.SetTextureOffset(targetName, data.textureOffset);
                    break;
            }
        }
        catch
        {
            // Ignore type mismatches
        }
    }

    private void Log(string msg)
    {
        _logOutput += msg + "\n";
    }
}
