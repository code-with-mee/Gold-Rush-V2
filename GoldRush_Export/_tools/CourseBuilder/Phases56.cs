using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class CourseBuilder
{
    public static Object Asset(string path) => AssetDatabase.LoadMainAssetAtPath(path.StartsWith("Assets/") ? path : G + path);
    public static Material Mat(string n) => AssetDatabase.LoadAssetAtPath<Material>(G + "Materials/" + n + ".mat");

    /// <summary>Edit a prefab file like Prefab Mode does.</summary>
    public static void EditPrefab(string path, System.Action<GameObject> edit)
    {
        var root = PrefabUtility.LoadPrefabContents(G + path);
        edit(root);
        PrefabUtility.SaveAsPrefabAsset(root, G + path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ================================================================ PHASE 5
    public static void Phase5()
    {
        // step 12: GoldRushAssets
        var gaPath = G + "Resources/GoldRushAssets.asset";
        var ga = AssetDatabase.LoadMainAssetAtPath(gaPath);
        if (!ga) { ga = ScriptableObject.CreateInstance(T("GameAssets")); AssetDatabase.CreateAsset(ga, gaPath); }
        Set(ga, "lit", Mat("GR_Lit")); Set(ga, "litEmissive", Mat("GR_LitEmissive"));
        Set(ga, "litTransparent", Mat("GR_LitTransparent")); Set(ga, "particle", Mat("GR_Particle")); Set(ga, "skybox", Mat("GR_SunsetSky"));
        var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { G + "Art/Audio" }).Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(c => c.name).ToList();
        Log("sound clips: " + clips.Count + " " + string.Join(",", clips.Select(c => c.name)));
        Set(ga, "sounds", clips);
        EditorUtility.SetDirty(ga);
        AssetDatabase.SaveAssets();

        // step 13: Coin prefab
        EditPrefab("Prefabs/Gameplay/Coin.prefab", root =>
        {
            var c = Add(root, "Coin");
            Set(c, "visual", root.transform.Find("Visual"));
            if (root.GetComponentInChildren<Collider>()) Warn("Coin prefab has a collider");
        });

        // step 14: LevelScene + CoinSpots + coins
        Open("Level1");
        var lvl = Root("Level 1 - Training Yard");
        Set(Add(lvl, "LevelScene"), "arenaHalf", 16f);
        for (int i = 1; i <= 6; i++)
        {
            var spot = Find("Level 1 - Training Yard/Gameplay/Coins/CoinSpot " + i);
            Add(spot.gameObject, "CoinSpot");
            Prefab("Prefabs/Gameplay/Coin.prefab", spot, new Vector3(0, 1, 0));
        }
        // step 15: GameManager + build list
        var gm = new GameObject("GameManager");
        Add(gm, "GameManager");
        Save();
        BuildScenes("Level1");
        Validate();
        ExportPackage("Phase05_Coins");
        Done();
    }

    // ================================================================ PHASE 6
    public static void Phase6()
    {
        // step 5: ExitGate script on the provided prefab
        EditPrefab("Prefabs/Gameplay/ExitGate.prefab", root =>
        {
            var g = Add(root, "ExitGate");
            Transform C(string n) { var t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == n); if (!t) Warn("gate child missing " + n); return t; }
            Set(g, "door", C("Door")); Set(g, "glow", C("Glow")); Set(g, "glowRing", C("Ring")); Set(g, "markerAnchor", C("MarkerAnchor"));
            Set(g, "glowLight", C("GlowLight").GetComponent<Light>());
            Set(g, "text", C("Text").GetComponent<TextMesh>());
            Set(g, "strips", new System.Collections.Generic.List<Object> { C("StripTop").GetComponent<Renderer>(), C("StripLeft").GetComponent<Renderer>(), C("StripRight").GetComponent<Renderer>() });
            Set(g, "greenStrip", Mat("M_GlowGreen"));
            if (C("Glow").gameObject.activeSelf) Warn("Glow is active in the gate prefab");
            if (C("GlowLight").GetComponent<Light>().enabled) Warn("GlowLight light enabled in the gate prefab");
            var b = root.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
            Log($"gate bounds size {b.size} center {b.center}");
            foreach (var n in new[] { "PostLeft", "PostRight" }) { var t = C(n); var bb = t.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; }); Log($"{n} x {bb.min.x:F2}..{bb.max.x:F2}"); }
            var pad = C("Glow"); Log("Glow local pos " + pad.localPosition);
        });
        // step 6: place the gate
        Open("Level1");
        var gp = Find("Level 1 - Training Yard/Gameplay");
        var spot = Find("Level 1 - Training Yard/Gameplay/ExitGateSpot");
        var gate = Prefab("Prefabs/Gameplay/ExitGate.prefab", gp, spot.localPosition);
        Object.DestroyImmediate(spot.gameObject);
        foreach (var t in gate.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        Save();
        Validate();
        ExportPackage("Phase06_Gate");
        Done();
    }
}
