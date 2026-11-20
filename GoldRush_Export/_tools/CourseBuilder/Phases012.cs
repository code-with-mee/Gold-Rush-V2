using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class CourseBuilder
{
    // ================================================================ PHASE 0
    public static void Phase0()
    {
        // step 8: folders
        foreach (var f in new[] { "Animations", "Scenes", "Scripts/Core", "Scripts/Audio", "Scripts/World", "Scripts/Level", "Scripts/Gameplay", "Scripts/UI" })
            Folder(G + f);
        // step 9: Sandbox scene
        var sc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(sc, G + "Scenes/Sandbox.unity");
        Prefab("Prefabs/Level/Floor_40m.prefab", null, Vector3.zero);
        Prefab("Art/Characters/Hero/Hero.fbx", null, Vector3.zero).transform.localScale = Vector3.one * 0.36f;
        var cam = Root("Main Camera");
        cam.transform.SetPositionAndRotation(new Vector3(0, 1.5f, -4), Quaternion.Euler(10, 0, 0));
        Save();
        Shot("00", "sandbox-hero.jpg", new Vector3(0, 1.5f, -4), new Vector3(10, 0, 0));
        Validate();
        ExportPackage("Phase00_Project");
    }

    // ================================================================ PHASE 1
    public static void Phase1()
    {
        Open("Sandbox");
        Prefab("Prefabs/Level/Block_1x1x1.prefab", null, new Vector3(-2, 0, 1));
        Prefab("Prefabs/Level/Block_1x1x1.prefab", null, new Vector3(-3.5f, 0, 1));
        Prefab("Prefabs/Level/Block_1x1x1.prefab", null, new Vector3(-2.75f, 0, 2.5f));
        Prefab("Prefabs/Level/Wall_8m.prefab", null, new Vector3(0, 0, 5));
        Prefab("Prefabs/Level/Pillar.prefab", null, new Vector3(2.5f, 0, 1.5f));
        Prefab("Prefabs/Level/LampPost.prefab", null, new Vector3(4.5f, 0, 2));
        var cam = Root("Main Camera");
        cam.transform.SetPositionAndRotation(new Vector3(0, 3, -8), Quaternion.Euler(15, 0, 0));
        Save();
        Shot("01", "sandbox-kit.jpg", new Vector3(0, 3, -8), new Vector3(15, 0, 0));
        Validate();
        ExportPackage("Phase01_Prefabs");
    }

    // ================================================================ PHASE 2
    public static void Phase2()
    {
        var sc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(sc, G + "Scenes/Level1.unity");
        var gv = Root("Global Volume"); if (gv) Object.DestroyImmediate(gv);

        // step 4: groups
        var root = Empty("Level 1 - Training Yard", null).transform;
        var stat = Empty("Static", root).transform;
        var floor = Empty("Floor", stat).transform;
        var walls = Empty("Walls", stat).transform;
        var lamps = Empty("Lamps", stat).transform;
        var obst = Empty("Obstacles", stat).transform;
        var scen = Empty("Scenery", root).transform;
        var gp = Empty("Gameplay", root).transform;
        var coins = Empty("Coins", gp).transform;
        var mons = Empty("Monsters", gp).transform;
        var pups = Empty("PowerUps", gp).transform;

        // step 6: floor
        Prefab("Prefabs/Level/Floor_40m.prefab", floor, Vector3.zero);
        Prefab("Prefabs/Level/OuterGround.prefab", scen, Vector3.zero);

        // steps 7-9: walls
        Transform Side(string name, Vector3 pos, float rotY, bool north)
        {
            var s = Empty(name, walls, pos, rotY).transform;
            foreach (var x in north ? new[] { -12f, 12f } : new[] { -12f, -4f, 4f, 12f })
                Prefab("Prefabs/Level/Wall_8m.prefab", s, new Vector3(x, 0, 0));
            if (north) { Prefab("Prefabs/Level/Wall_4m.prefab", s, new Vector3(-6, 0, 0)); Prefab("Prefabs/Level/Wall_4m.prefab", s, new Vector3(6, 0, 0)); }
            return s;
        }
        Side("WallSouth", new Vector3(0, 0, -16.5f), 0, false);
        Side("WallNorth", new Vector3(0, 0, 16.5f), 0, true);
        var blocker = Empty("GateBlocker", walls, new Vector3(0, 2.5f, 17.25f));
        blocker.AddComponent<BoxCollider>().size = new Vector3(8, 5, 1);
        Side("WallEast", new Vector3(16.5f, 0, 0), 90, false);
        Side("WallWest", new Vector3(-16.5f, 0, 0), 90, false);
        var corners = Empty("Corners", walls).transform;
        foreach (var p in new[] { new Vector3(-16.5f, 0, -16.5f), new Vector3(16.5f, 0, -16.5f), new Vector3(-16.5f, 0, 16.5f), new Vector3(16.5f, 0, 16.5f) })
            Prefab("Prefabs/Level/Pillar.prefab", corners, p);

        // step 10: lamps
        foreach (var p in new[] { new Vector3(-14.5f, 0, -14.5f), new Vector3(14.5f, 0, -14.5f), new Vector3(-14.5f, 0, 14.5f), new Vector3(14.5f, 0, 14.5f),
                                  new Vector3(-14.5f, 0, 0), new Vector3(14.5f, 0, 0), new Vector3(-4.5f, 0, -14.5f), new Vector3(4.5f, 0, -14.5f) })
            Prefab("Prefabs/Level/LampPost.prefab", lamps, p);

        // step 11: obstacles
        (string, Vector3, float)[] ob =
        {
            ("AccentBlock_Teal", new Vector3(4, 0, 4.5f), 0), ("Block_4x1x2", new Vector3(7, 0, 4.5f), 0), ("Block_4x1x2", new Vector3(11, 0, 4.5f), 0),
            ("Block_4x1x2", new Vector3(4, 0, 7.5f), 90), ("Pillar", new Vector3(-12.5f, 0, 4.5f), 0), ("Pillar", new Vector3(-11.5f, 0, -5), 0),
            ("Pillar", new Vector3(-5, 0, 12.5f), 0), ("Block_2x2x2", new Vector3(-11, 0, 10.5f), 0), ("Block_2x2x2", new Vector3(-6.5f, 0, 1.5f), 0),
            ("Block_1x1x1", new Vector3(-6.5f, 2, 1.5f), 0), ("Block_4x1x2", new Vector3(6, 0, -6), 90), ("Block_1x1x1", new Vector3(-6.5f, 0, -9.5f), 0),
            ("Barrier_Orange", new Vector3(-2.5f, 0, -5), 0), ("AccentBlock_Red", new Vector3(10.5f, 0, -10), 0), ("AccentBlock_Teal", new Vector3(12, 0, -3.5f), 0),
            ("Terminal", new Vector3(10.5f, 0, 12), 90), ("AccentBlock_Red", new Vector3(-7.5f, 0, 7), 0),
        };
        foreach (var (n, p, r) in ob) Prefab("Prefabs/Level/" + n + ".prefab", obst, p, r);

        // step 12: markers
        Icon(Empty("PlayerSpawn", gp, new Vector3(0, 0, -9)), 1);
        Vector3[] cs = { new Vector3(5, 0, 11.5f), new Vector3(-6.5f, 0, -12.5f), new Vector3(14.5f, 0, -7.5f), new Vector3(-7.5f, 0, 0), new Vector3(-14.5f, 0, 12.5f), new Vector3(5, 0, -2) };
        for (int i = 0; i < cs.Length; i++) Icon(Empty("CoinSpot " + (i + 1), coins, cs[i]), 4);
        Icon(Empty("ExitGateSpot", gp, new Vector3(0, 0, 16)), 3);
        Icon(Empty("Slime 1", mons, new Vector3(5.5f, 0, 10.5f)), 6);
        Icon(Empty("Slime 2", mons, new Vector3(14, 0, -9.5f)), 6);
        Icon(Empty("PowerUp Magnet", pups, new Vector3(-14.5f, 0, -8.5f)), 7);

        // step 13: sun
        var sun = Root("Directional Light");
        sun.name = "Sun";
        sun.transform.rotation = Quaternion.Euler(30, 35, 0);
        var l = sun.GetComponent<Light>();
        l.color = Hex("FFDEAD"); l.intensity = 1.5f;
        l.shadows = LightShadows.Soft; l.shadowStrength = 0.75f;
        l.shadowBias = 0.05f; l.shadowNormalBias = 0.4f;
        var ald = Get(sun, "UnityEngine.Rendering.Universal.UniversalAdditionalLightData");
        if (ald) Set(ald, "m_UsePipelineSettings", false);

        // step 14: sky, ambient, fog
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(G + "Materials/GR_SunsetSky.mat");
        RenderSettings.sun = l;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("CCB29E");
        RenderSettings.ambientEquatorColor = Hex("BD8F6B");
        RenderSettings.ambientGroundColor = Hex("75543D");
        RenderSettings.fog = true; RenderSettings.fogColor = Hex("F5C999");
        RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 120;

        // step 15: static
        foreach (var t in stat.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

        // step 16: overview camera
        Root("Main Camera").transform.SetPositionAndRotation(new Vector3(0, 22, -30), Quaternion.Euler(35, 0, 0));
        Save();

        Shot("02", "level1-top.jpg", new Vector3(0, 60, 0), new Vector3(90, 0, 0), 1200, 1200, true, 19);
        Shot("02", "level1-overview.jpg", new Vector3(0, 22, -30), new Vector3(35, 0, 0));
        Shot("02", "level1-from-spawn.jpg", new Vector3(0, 1.5f, -9), new Vector3(5, 0, 0));
        Validate();
        ExportPackage("Phase02_Level");
    }

    public static void Phases012() { Phase0(); Phase1(); Phase2(); Done(); }
}
