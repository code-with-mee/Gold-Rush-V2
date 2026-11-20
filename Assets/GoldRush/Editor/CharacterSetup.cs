using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GoldRush.EditorTools
{
    /// <summary>Builds one AnimatorController per character from the clips inside its FBX.
    /// Every controller has the same parameters (see AnimParams): a "Locomotion" blend tree driven by
    /// Speed (idle → walk → run) plus Attack / Hit / Win / Die states triggered from Any State.</summary>
    public static class CharacterSetup
    {
        const string Dir = "Assets/GoldRush/Art/Characters/";
        const string ControllerDir = Dir + "Controllers/";

        struct Spec
        {
            public string name, fbx, idle, walk, run, attack, hit, win, die;
        }

        static readonly Spec Hero = new Spec { name = "Hero", fbx = Dir + "Hero/Hero.fbx", idle = "Idle", walk = "Walk", run = "Run", attack = "Punch", hit = "HitReact", win = "Wave", die = "Death" };
        static readonly Spec Slime = new Spec { name = "Slime", fbx = Dir + "Monsters/Slime.fbx", idle = "Idle", walk = "Walk", run = "Walk", attack = "Bite_Front", hit = "HitRecieve", win = "Dance", die = "Death" };
        static readonly Spec Ghost = new Spec { name = "Ghost", fbx = Dir + "Monsters/Ghost.fbx", idle = "Flying_Idle", walk = "Fast_Flying", run = "Fast_Flying", attack = "Headbutt", hit = "HitReact", win = "Yes", die = "Death" };
        static readonly Spec King = new Spec { name = "MushroomKing", fbx = Dir + "Monsters/MushroomKing.fbx", idle = "Idle", walk = "Walk", run = "Run", attack = "Punch", hit = "HitReact", win = "Wave", die = "Death" };

        /// <summary>Called from GoldRushSetup.Setup(). Fills the CharacterDefs on the GameAssets.</summary>
        public static void Build(GameAssets ga)
        {
            System.IO.Directory.CreateDirectory(ControllerDir);
            Fill(ga.hero, Hero, 1.35f, 1.9f, 0.74f);
            Fill(ga.slime, Slime, 0.95f, 1f, 1f);
            Fill(ga.ghost, Ghost, 1.3f, 1f, 1f);
            Fill(ga.mushroomKing, King, 2.6f, 1f, 1f);
        }

        static void Fill(CharacterDef def, Spec spec, float height, float hatScale, float hatHeight)
        {
            def.model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.fbx);
            def.controller = BuildController(spec);
            def.height = height;
            def.hatScale = hatScale;
            def.hatHeight = hatHeight;
            if (!def.model) { Debug.LogError("[GoldRush] Missing character model " + spec.fbx); return; }
            def.walkSpeed = MeasureGroundSpeed(def, Clip(spec.fbx, spec.walk));
            def.runSpeed = spec.run == spec.walk ? def.walkSpeed * 1.6f : MeasureGroundSpeed(def, Clip(spec.fbx, spec.run));
            Debug.Log($"[GoldRush] {spec.name}: walk clip matches {def.walkSpeed:F2} m/s, run clip {def.runSpeed:F2} m/s");
        }

        /// <summary>
        /// How fast (m/s) a walk/run clip was animated to move, measured from the feet: while a foot is planted
        /// it slides backwards relative to the body by one stride, which takes about half a cycle.
        /// Returns 0 when the model has no feet (e.g. the slime or ghost) — runtime then estimates from height.
        /// </summary>
        static float MeasureGroundSpeed(CharacterDef def, AnimationClip clip)
        {
            if (!clip || clip.length <= 0f) return 0f;
            var holder = new GameObject("MeasureStride").transform;
            try
            {
                var anim = CharacterFactory.BuildModel(def, holder);
                var feet = anim.GetComponentsInChildren<Transform>()
                    .Where(t => { var n = t.name.ToLowerInvariant(); return n.Contains("foot") && !n.Contains("toe") && !n.Contains("end"); })
                    .ToArray();
                if (feet.Length == 0) return 0f;
                float stride = 0f;
                foreach (var foot in feet)
                {
                    float min = float.MaxValue, max = float.MinValue;
                    for (int i = 0; i <= 60; i++)
                    {
                        clip.SampleAnimation(anim.gameObject, clip.length * i / 60f);
                        float z = holder.InverseTransformPoint(foot.position).z;
                        min = Mathf.Min(min, z); max = Mathf.Max(max, z);
                    }
                    stride = Mathf.Max(stride, max - min);
                }
                float speed = stride / (clip.length * 0.5f);
                return speed > 0.2f && speed < 15f ? speed : 0f;
            }
            finally { Object.DestroyImmediate(holder.gameObject); }
        }

        static AnimationClip Clip(string fbx, string name)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToArray();
            var clip = clips.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name));
            if (!clip) Debug.LogWarning($"[GoldRush] Clip '{name}' not found in {fbx}. Available: {string.Join(", ", clips.Select(c => c.name))}");
            return clip;
        }

        static RuntimeAnimatorController BuildController(Spec s)
        {
            string path = ControllerDir + s.name + ".controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Win", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var loco = ctrl.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip(s.fbx, s.idle), 0f);
            tree.AddChild(Clip(s.fbx, s.walk), 0.5f);
            tree.AddChild(Clip(s.fbx, s.run), 1f);
            sm.defaultState = loco;

            Action(sm, loco, "Attack", Clip(s.fbx, s.attack), 0.85f);
            Action(sm, loco, "Hit", Clip(s.fbx, s.hit), 0.85f);
            Action(sm, loco, "Win", Clip(s.fbx, s.win), 3f);  // win clip loops: wave a few times, then idle
            Action(sm, null, "Die", Clip(s.fbx, s.die), 0f);  // stays on the last frame

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static void Action(AnimatorStateMachine sm, AnimatorState back, string trigger, AnimationClip clip, float exitTime)
        {
            var st = sm.AddState(trigger);
            st.motion = clip;
            var t = sm.AddAnyStateTransition(st);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.canTransitionToSelf = false;
            if (back != null)
            {
                var b = st.AddTransition(back);
                b.hasExitTime = true;
                b.exitTime = exitTime;
                b.duration = 0.2f;
            }
        }
    }
}
