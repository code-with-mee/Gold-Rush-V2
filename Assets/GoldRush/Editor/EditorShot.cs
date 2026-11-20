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

        [Serializable] class Shot { public string shot, window, select, scene; }
        [Serializable] class ShotList { public Shot[] items; }

        /// <summary>Takes the pictures listed in Temp/keyshots.json (course key shots) into GoldRush_Export/Docs/images/.</summary>
        public static void TakeKeyShots()
        {
            var json = "{\"items\":" + File.ReadAllText("Temp/keyshots.json") + "}";
            var list = JsonUtility.FromJson<ShotList>(json).items;
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../GoldRush_Export/Docs/images/"));
            int i = 0;
            var log = new System.Text.StringBuilder();
            void Next()
            {
                if (i >= list.Length)
                {
                    File.WriteAllText("Temp/keyshots.done", log.ToString());
                    return;
                }
                var s = list[i++];
                string scenePath = "Assets/GoldRush/Scenes/" + s.scene + ".unity";
                if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != scenePath)
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                string kind = s.window;
                UnityEngine.Object target = null;
                if (s.select.StartsWith("Assets/"))
                {
                    target = AssetDatabase.LoadMainAssetAtPath(s.select);
                    if (kind == "hierarchy") kind = "project"; // an asset lives in the Project window
                }
                else target = FindPath(s.select);
                if (target) { Selection.activeObject = target; EditorGUIUtility.PingObject(target); }
                if (kind == "scene" && SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.FrameSelected();
                var w = Find(kind);
                if (w) { w.Show(); w.Focus(); }
                ActiveEditorTracker.sharedTracker.ForceRebuild(); // the Inspector shows the new selection
                log.AppendLine($"{s.shot} {kind} {(target ? target.name : "NOT FOUND " + s.select)}");
                double until = EditorApplication.timeSinceStartup + 1.5; // let Unity draw the window for real
                void Wait()
                {
                    if (w) w.Repaint();
                    if (EditorApplication.timeSinceStartup < until) return;
                    EditorApplication.update -= Wait;
                    CaptureLater(w, outDir + s.shot + ".png", Next, 20);
                }
                EditorApplication.update += Wait;
            }
            Next();
        }

        /// <summary>Finds a scene object by its Hierarchy path; if part of the path is missing, the closest parent.</summary>
        static GameObject FindPath(string path)
        {
            var parts = path.Split('/');
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Transform t = scene.GetRootGameObjects().Select(g => g.transform).FirstOrDefault(g => g.name == parts[0]);
            if (!t) return null;
            for (int k = 1; k < parts.Length; k++)
            {
                var c = t.Find(parts[k]);
                if (!c) break;
                t = c;
            }
            return t.gameObject;
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
