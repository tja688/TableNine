using System;
using UnityEditor;

namespace QfPlayAgent.Editor
{
    internal static class PlayAgentEditorWait
    {
        public static void WaitFrames(int frames, int timeoutMs)
        {
            frames = Math.Max(0, frames);
            timeoutMs = Math.Max(100, timeoutMs);
            if (frames == 0)
            {
                return;
            }

            var startedAt = EditorApplication.timeSinceStartup;
            var deadline = startedAt + timeoutMs / 1000d;

            for (var i = 0; i < frames; i++)
            {
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    return;
                }

                // MCP custom tools run on the Unity main thread; never block on Thread.Sleep
                // or EditorApplication.update will never fire.
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
    }
}
