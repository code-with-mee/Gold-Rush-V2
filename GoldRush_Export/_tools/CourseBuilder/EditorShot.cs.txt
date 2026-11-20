using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GoldRush.EditorTools
{
    /// <summary>
    /// Saves pictures of editor windows (Hierarchy, Inspector, Scene, Project) for the course guides.
    /// Unity draws the window into a texture itself, so no screen-recording permission is needed.
    /// </summary>
    public static class EditorShot
    {
        static readonly Type HierarchyType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        static readonly Type InspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        static readonly Type ProjectType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");

        public static bool Capture(EditorWindow w, string path)
        {
            if (!w) return false;
            w.Repaint();
            var parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
            var parent = parentField?.GetValue(w);
            if (parent == null) { Debug.LogWarning("[GoldRush] EditorShot: no parent view"); return false; }
            var grab = parent.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "GrabPixels" && m.GetParameters().Length == 2);
            if (grab == null) { Debug.LogWarning("[GoldRush] EditorShot: GrabPixels not found"); return false; }
            var pos = w.position;
            float scale = EditorGUIUtility.pixelsPerPoint;
            int pw = Mathf.RoundToInt(pos.width * scale), ph = Mathf.RoundToInt(pos.height * scale);
            var rt = new RenderTexture(pw, ph, 0, RenderTextureFormat.ARGB32);
            // rect inside the parent view, in pixels
            var rect = new Rect(0, 0, pw, ph);
            grab.Invoke(parent, new object[] { rt, rect });
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            return true;
        }

        /// <summary>Editor window type names for each screenshot kind (data-window in the guides).</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> Kinds = new System.Collections.Generic.Dictionary<string, string>
        {
            { "hierarchy", "UnityEditor.SceneHierarchyWindow" }, { "inspector", "UnityEditor.InspectorWindow" },
            { "project", "UnityEditor.ProjectBrowser" }, { "scene", "UnityEditor.SceneView" }, { "game", "UnityEditor.GameView" },
            { "animator", "UnityEditor.Graphs.AnimatorControllerTool" }, { "lighting", "UnityEditor.LightingWindow" },
            { "buildprofiles", "UnityEditor.Build.Profile.BuildProfileWindow" }, { "console", "UnityEditor.ConsoleWindow" },
        };

        public static System.Type WindowType(string kind)
        {
            if (!Kinds.TryGetValue(kind, out var name)) return null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>Finds (or opens) the editor window for a screenshot kind.</summary>
        public static EditorWindow Find(string kind, bool open = true)
        {
            var t = WindowType(kind);
            if (t == null) return null;
            var w = Resources.FindObjectsOfTypeAll(t).FirstOrDefault() as EditorWindow;
            if (!w && open) w = EditorWindow.GetWindow(t);
            return w;
        }

        /// <summary>Repaints the window, waits a few editor frames (so it is really drawn), then saves it.</summary>
        public static void CaptureLater(EditorWindow w, string path, Action done = null, int frames = 6)
        {
            if (!w) { done?.Invoke(); return; }
            w.Repaint();
            int left = frames;
            void Tick()
            {
                w.Repaint();
                if (--left > 0) return;
                EditorApplication.update -= Tick;
                Capture(w, path);
                done?.Invoke();
            }
            EditorApplication.update += Tick;
        }

        /// <summary>Test: saves the Hierarchy, Inspector and Project windows to Temp/editorshots/.</summary>
        public static void TestCapture()
        {
            var cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam) Selection.activeGameObject = cam.gameObject;
            EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
            var kinds = new[] { "hierarchy", "inspector", "project", "scene" };
            int i = 0;
            void Next()
            {
                if (i >= kinds.Length) { Debug.Log("[GoldRush] EditorShot test done"); return; }
                string k = kinds[i++];
                var w = Find(k);
                Debug.Log($"[GoldRush] EditorShot {k}: {(w ? w.position.ToString() : "no window")}");
                CaptureLater(w, "Temp/editorshots/" + k + ".png", Next);
            }
            Next();
        }
    }
}
