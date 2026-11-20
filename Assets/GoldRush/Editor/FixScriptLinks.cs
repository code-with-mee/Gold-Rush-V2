using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GoldRush.EditorTools
{
    /// <summary>
    /// Reconnects scripts in imported scenes / prefabs to the scripts YOU made.
    /// Every script file has a hidden ID (GUID, in its .meta file). Scenes remember scripts by that ID. When you type
    /// a script yourself it gets a new ID, so a scene from the course package would show "Missing (Mono Script)".
    /// This tool looks up which class each missing ID was (ScriptLinks.json) and points it at your script with the
    /// same class name. It runs by itself after you import a package, or use menu: Gold Rush > Fix Script Links.
    /// </summary>
    public class FixScriptLinks : AssetPostprocessor
    {
        static readonly Regex ScriptRef = new Regex(@"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}");

        [System.Serializable] class Link { public string guid, type; }
        [System.Serializable] class LinkFile { public Link[] links; }

        [MenuItem("Gold Rush/Fix Script Links", priority = 100)]
        public static void FixAll() => Run(true);

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Any(p => p.EndsWith(".unity") || p.EndsWith(".prefab")))
                EditorApplication.delayCall += () => Run(false);
        }

        static void Run(bool verbose)
        {
            var mapFile = AssetDatabase.FindAssets("ScriptLinks t:TextAsset").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.EndsWith("ScriptLinks.json"));
            if (mapFile == null) { if (verbose) Debug.LogWarning("[GoldRush] ScriptLinks.json not found"); return; }
            var map = JsonUtility.FromJson<LinkFile>(File.ReadAllText(mapFile)).links.ToDictionary(l => l.guid, l => l.type);

            // the scripts in THIS project, by class name
            var mine = new Dictionary<string, string>();
            foreach (var g in AssetDatabase.FindAssets("t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var cls = ms ? ms.GetClass() : null;
                if (cls != null && !mine.ContainsKey(cls.FullName)) mine[cls.FullName] = g;
            }

            int files = 0, refs = 0;
            var missing = new HashSet<string>();
            foreach (var file in Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories)
                         .Where(f => f.EndsWith(".unity") || f.EndsWith(".prefab") || f.EndsWith(".asset")))
            {
                string text = File.ReadAllText(file);
                if (!text.Contains("m_Script:")) continue;
                bool changed = false;
                text = ScriptRef.Replace(text, m =>
                {
                    string g = m.Groups[1].Value;
                    if (!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(g))) return m.Value; // script exists
                    if (!map.TryGetValue(g, out var type)) return m.Value;                        // not one of ours
                    if (!mine.TryGetValue(type, out var myGuid)) { missing.Add(type); return m.Value; }
                    changed = true; refs++;
                    return $"m_Script: {{fileID: 11500000, guid: {myGuid}, type: 3}}";
                });
                if (!changed) continue;
                File.WriteAllText(file, text);
                files++;
                AssetDatabase.ImportAsset(file.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
            }
            if (refs > 0 || verbose)
                Debug.Log($"[GoldRush] Fix Script Links: reconnected {refs} script references in {files} files." +
                          (missing.Count > 0 ? " Still missing (make these scripts first): " + string.Join(", ", missing) : ""));
        }
    }
}
