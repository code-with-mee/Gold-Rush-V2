// Course checkpoint builder: does by code what each phase guide tells the student to do.
// NEVER shipped: it lives outside Assets/GoldRush. Uses reflection / SerializedObject for game types,
// so it compiles in every phase.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class CourseBuilder
{
    public const string G = "Assets/GoldRush/";
    public const string Export = "/Users/kingly/Desktop/M14/GoldRushDemo/GoldRush_Export/";
    public static readonly List<string> Problems = new List<string>();

    public static void Log(string s) { Debug.Log("[CB] " + s); }
    public static void Warn(string s) { Problems.Add(s); Debug.LogWarning("[CB] PROBLEM: " + s); }

    // ------------------------------------------------------------------ reflection helpers
    public static Type T(string name)
    {
        if (!name.Contains(".")) name = "GoldRush." + name;
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = a.GetType(name);
            if (t != null) return t;
        }
        Warn("type not found: " + name);
        return null;
    }

    public static Component Add(GameObject go, string type)
    {
        var t = T(type);
        if (t == null) return null;
        var c = go.GetComponent(t);
        return c ? c : go.AddComponent(t);
    }

    public static Component Get(GameObject go, string type) { var t = T(type); return t == null ? null : go.GetComponent(t); }

    /// <summary>Set a serialized field by its Inspector path (field name). Supports the common value types.</summary>
    public static void Set(Object o, string prop, object v)
    {
        if (!o) { Warn("Set on null " + prop); return; }
        var so = new SerializedObject(o);
        var p = so.FindProperty(prop);
        if (p == null) { Warn($"{o.GetType().Name}: no field '{prop}'"); return; }
        SetProp(p, v);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetProp(SerializedProperty p, object v)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Float: p.floatValue = Convert.ToSingle(v); break;
            case SerializedPropertyType.Integer: p.intValue = Convert.ToInt32(v); break;
            case SerializedPropertyType.Boolean: p.boolValue = (bool)v; break;
            case SerializedPropertyType.String: p.stringValue = (string)v; break;
            case SerializedPropertyType.Vector3: p.vector3Value = (Vector3)v; break;
            case SerializedPropertyType.Vector2: p.vector2Value = (Vector2)v; break;
            case SerializedPropertyType.Color: p.colorValue = (Color)v; break;
            case SerializedPropertyType.ObjectReference: p.objectReferenceValue = (Object)v; break;
            case SerializedPropertyType.Enum:
                if (v is string s)
                {
                    int i = Array.IndexOf(p.enumNames, s);
                    if (i < 0) Warn($"enum {p.name}: no value {s}");
                    p.enumValueIndex = i;
                }
                else p.enumValueIndex = Convert.ToInt32(v);
                break;
            case SerializedPropertyType.Generic when p.isArray && v is System.Collections.IList list:
                p.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++) SetProp(p.GetArrayElementAtIndex(i), list[i]);
                break;
            default: Warn($"unsupported property type {p.propertyType} for {p.name}"); break;
        }
    }

    // ------------------------------------------------------------------ scene helpers
    public static GameObject Empty(string name, Transform parent, Vector3 pos = default, float rotY = 0)
    {
        var go = new GameObject(name);
        if (parent) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, rotY, 0);
        return go;
    }

    public static GameObject Prefab(string path, Transform parent, Vector3 pos, float rotY = 0, string rename = null)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path.StartsWith("Assets/") ? path : G + path);
        if (!asset) { Warn("missing prefab/model " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent ? parent.gameObject.scene : SceneManager.GetActiveScene());
        if (parent) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, rotY, 0);
        if (rename != null) go.name = rename;
        return go;
    }

    public static Transform Find(string path)
    {
        var parts = path.Split('/');
        foreach (var r in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (r.name != parts[0]) continue;
            if (parts.Length == 1) return r.transform;
            var t = r.transform.Find(string.Join("/", parts.Skip(1)));
            if (t) return t;
        }
        Warn("not found in scene: " + path);
        return null;
    }

    public static GameObject Root(string name) => SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);

    public static void Icon(GameObject go, int label)
    {
        var tex = EditorGUIUtility.IconContent("sv_label_" + label).image as Texture2D;
        EditorGUIUtility.SetIconForObject(go, tex);
    }

    public static Scene Open(string scene) => EditorSceneManager.OpenScene(G + "Scenes/" + scene + ".unity", OpenSceneMode.Single);
    public static void Save() { EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); }

    public static void Folder(string path)
    {
        var parts = path.TrimEnd('/').Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    public static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

    // ------------------------------------------------------------------ screenshots (editor camera render)
    public static void Shot(string phase, string file, Vector3 pos, Vector3 euler, int w = 1600, int h = 900, bool ortho = false, float orthoSize = 20)
    {
        var dir = Export + "Docs/images/phase-" + phase + "/";
        Directory.CreateDirectory(dir);
        var go = new GameObject("__ShotCam");
        var cam = go.AddComponent<Camera>();
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
        cam.fieldOfView = 55; cam.nearClipPlane = 0.1f; cam.farClipPlane = 600;
        cam.orthographic = ortho; cam.orthographicSize = orthoSize;
        cam.clearFlags = CameraClearFlags.Skybox;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(dir + file, tex.EncodeToJPG(88));
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
        Log("screenshot " + dir + file);
    }

    // ------------------------------------------------------------------ checks + export
    /// <summary>Missing scripts and missing (broken) object references in all scenes and prefabs under GoldRush.</summary>
    public static void Validate()
    {
        int bad = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GoldRush" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            bad += CheckObject(go, path);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/GoldRush" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var r in sc.GetRootGameObjects()) bad += CheckObject(r, path);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/GoldRush" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var o = AssetDatabase.LoadMainAssetAtPath(path);
            if (o) bad += CheckRefs(o, path);
        }
        Log(bad == 0 ? "VALIDATE OK: no missing scripts / references" : $"VALIDATE: {bad} problems");
    }

    static int CheckObject(GameObject root, string where)
    {
        int bad = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            int miss = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if (miss > 0) { bad++; Warn($"{where}: {Path(t)} has {miss} missing script(s)"); }
            foreach (var c in t.GetComponents<Component>()) if (c) bad += CheckRefs(c, where + ":" + Path(t));
        }
        return bad;
    }

    static int CheckRefs(Object o, string where)
    {
        int bad = 0;
        var so = new SerializedObject(o);
        var p = so.GetIterator();
        while (p.Next(true))
        {
            if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
            if (p.objectReferenceValue == null && !p.objectReferenceEntityIdValue.Equals(default(EntityId)))
            { bad++; Warn($"{where} {o.GetType().Name}.{p.propertyPath}: missing reference"); }
        }
        return bad;
    }

    static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;

    public static void ExportPackage(string name)
    {
        AssetDatabase.SaveAssets();
        string outPath = Export + "Packages/GoldRush_" + name + ".unitypackage";
        AssetDatabase.ExportPackage(new[] { "Assets/GoldRush" }, outPath, ExportPackageOptions.Recurse);
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/GoldRush/") && !AssetDatabase.IsValidFolder(p)).OrderBy(p => p);
        File.WriteAllLines(Export + "Packages/GoldRush_" + name + ".txt", paths);
        Log("EXPORTED " + outPath + " (" + new FileInfo(outPath).Length / 1024 + " KB)");
    }

    public static void Done()
    {
        Log(Problems.Count == 0 ? "DONE, no problems" : "DONE with problems:\n  " + string.Join("\n  ", Problems));
    }

    public static void BuildScenes(params string[] scenes)
    {
        EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(G + "Scenes/" + s + ".unity", true)).ToArray();
    }
}
