using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace QfPlayAgent.Editor
{
    internal static class PlayAgentObserveHelper
    {
        public static bool TryCaptureScreenshot(string filenameWithoutExtension, out string absolutePath, out string error, int? width = null, int? height = null)
        {
            absolutePath = null;
            error = null;

            try
            {
                var folder = Path.Combine(Application.dataPath, "QfPlayAgent", "AgentRuns", PlayAgentSession.SessionId);
                Directory.CreateDirectory(folder);

                var w = width ?? Screen.width;
                var h = height ?? Screen.height;
                if (w <= 0 || h <= 0)
                {
                    w = 1280;
                    h = 720;
                }

                absolutePath = Path.Combine(folder, filenameWithoutExtension + ".png");

                var camera = Camera.main;
                if (camera == null)
                {
                    camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
                }

                if (camera == null)
                {
                    error = "No camera found for screenshot.";
                    return false;
                }

                var previousTarget = camera.targetTexture;
                var renderTexture = new RenderTexture(w, h, 24);
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var texture = new Texture2D(w, h, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                texture.Apply();

                camera.targetTexture = previousTarget;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(renderTexture);

                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string ToProjectRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return absolutePath;
            }

            var dataPath = Application.dataPath.Replace('\\', '/');
            var normalized = absolutePath.Replace('\\', '/');
            if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + normalized.Substring(dataPath.Length);
            }

            return absolutePath;
        }
    }
}
