using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static partial class CourseBuilder
{
    // ------------------------------------------------------------------ model import (phase 3 step 2-3, phase 7 step 4)
    public static void ImportCharacter(string fbx, string[] loops, string[] used)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(G + fbx);
        mi.importCameras = false; mi.importLights = false;
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
        mi.importAnimation = true;
        var clips = (mi.clipAnimations != null && mi.clipAnimations.Length > 0) ? mi.clipAnimations : mi.defaultClipAnimations;
        bool renamed = false;
        foreach (var c in clips)
        {
            int bar = c.name.LastIndexOf('|');
            if (bar >= 0) { c.name = c.name.Substring(bar + 1); renamed = true; }
            if (loops.Contains(c.name) && (!c.loopTime || !c.loopPose)) { Log($"{fbx}: {c.name} loop was off, set on"); c.loopTime = c.loopPose = true; }
            if (used.Contains(c.name) && !loops.Contains(c.name) && c.loopTime) { Log($"{fbx}: {c.name} looped, set off"); c.loopTime = c.loopPose = false; }
        }
        if (renamed) Log(fbx + ": clip names had the CharacterArmature| prefix");
        foreach (var u in used) if (!clips.Any(c => c.name == u)) Warn($"{fbx}: clip {u} not found");
        mi.clipAnimations = clips;
        mi.SaveAndReimport();
    }

    public static AnimationClip Clip(string fbx, string name) =>
        AssetDatabase.LoadAllAssetsAtPath(G + fbx).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);

    /// <summary>Animator Controller exactly as phase 3 steps 4-7 describe.</summary>
    public static AnimatorController BuildController(string name, string fbx, string idle, string walk, string run, string attack, string hit, string win, string die)
    {
        string path = G + "Animations/" + name + ".controller";
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        foreach (var p in new[] { "Attack", "Hit", "Win", "Die" }) ctrl.AddParameter(p, AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        var loco = ctrl.CreateBlendTreeInController("Locomotion", out var tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(Clip(fbx, idle), 0f);
        tree.AddChild(Clip(fbx, walk), 0.5f);
        tree.AddChild(Clip(fbx, run), 1f);
        sm.defaultState = loco;
        void Action(string trig, string clip, float exit)
        {
            var st = sm.AddState(trig);
            st.motion = Clip(fbx, clip);
            if (!st.motion) Warn($"{name}: clip {clip} missing");
            var t = sm.AddAnyStateTransition(st);
            t.AddCondition(AnimatorConditionMode.If, 0, trig);
            t.hasExitTime = false; t.duration = 0.1f; t.canTransitionToSelf = false;
            if (exit > 0)
            {
                var b = st.AddTransition(loco);
                b.hasExitTime = true; b.exitTime = exit; b.duration = 0.2f;
            }
        }
        Action("Attack", attack, 0.85f);
        Action("Hit", hit, 0.85f);
        Action("Win", win, 3f);
        Action("Die", die, 0f);
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    public static void SetLayerRecursive(GameObject go, int layer) { foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }

    // ================================================================ PHASE 3
    public static void Phase3()
    {
        ImportCharacter("Art/Characters/Hero/Hero.fbx", new[] { "Idle", "Walk", "Run", "Wave" }, new[] { "Idle", "Walk", "Run", "Wave", "Punch", "HitReact", "Death" });
        var ctrl = BuildController("Hero", "Art/Characters/Hero/Hero.fbx", "Idle", "Walk", "Run", "Punch", "HitReact", "Wave", "Death");

        Open("Level1");
        var spawn = Find("Level 1 - Training Yard/Gameplay/PlayerSpawn");
        Add(spawn.gameObject, "PlayerSpawn");

        // step 11: Player
        var player = new GameObject("Player");
        var cc = player.AddComponent<CharacterController>();
        cc.slopeLimit = 50; cc.stepOffset = 0.35f; cc.skinWidth = 0.05f;
        cc.center = new Vector3(0, 0.695f, 0); cc.radius = 0.36f; cc.height = 1.35f;
        // step 12: model
        var model = Prefab("Art/Characters/Hero/Hero.fbx", player.transform, Vector3.zero, 0, "Model");
        model.transform.localScale = Vector3.one * 0.36f;
        var anim = model.GetComponent<Animator>();
        if (!anim) { Warn("Hero model has no Animator"); anim = model.AddComponent<Animator>(); }
        anim.runtimeAnimatorController = ctrl;
        if (!anim.avatar) Warn("Hero Animator has no Avatar");
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // step 13
        var pm = Add(player, "PlayerMotor");
        SetLayerRecursive(player, 2);
        Folder(G + "Prefabs/Gameplay");
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, G + "Prefabs/Gameplay/Player.prefab", InteractionMode.AutomatedAction);
        // step 14
        player.transform.position = spawn.position;
        var cam = Root("Main Camera");
        cam.transform.SetPositionAndRotation(new Vector3(0, 16, -26), Quaternion.Euler(35, 0, 0));
        var hb = model.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
        Log($"hero model bounds min y {hb.min.y:F2} max y {hb.max.y:F2} (capsule 0..1.35)");
        Save();
        Validate();
        ExportPackage("Phase03_Player");
        Done();
    }

    // ================================================================ PHASE 4
    public static void Phase4()
    {
        Open("Level1");
        var cam = Root("Main Camera");
        var c = cam.GetComponent<Camera>();
        c.fieldOfView = 55; c.nearClipPlane = 0.1f; c.farClipPlane = 400;
        if (!cam.GetComponent<AudioListener>()) Warn("Main Camera has no AudioListener");
        var tpc = Add(cam, "ThirdPersonCamera");
        var player = Root("Player");
        Set(tpc, "target", player.transform);
        Set(Get(player, "PlayerMotor"), "cam", tpc);
        if (player.layer != 2) Warn("Player not on Ignore Raycast");
        Save();
        Validate();
        ExportPackage("Phase04_Camera");
        Done();
    }
}
