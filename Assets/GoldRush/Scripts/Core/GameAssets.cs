using UnityEngine;

namespace GoldRush
{
    /// <summary>A playable/enemy character: imported model + generated animator controller.</summary>
    [System.Serializable]
    public class CharacterDef
    {
        public GameObject model;
        public RuntimeAnimatorController controller;
        [Tooltip("Model is scaled so its total height matches this (metres).")]
        public float height = 1.4f;
        [Tooltip("Hat size multiplier (hats are built for a ~25cm wide head).")]
        public float hatScale = 1f;
        [Tooltip("Where the hat sits, as a fraction of the model height.")]
        public float hatHeight = 0.9f;
        [Tooltip("Ground speed (m/s) the WALK clip was animated for, measured from the feet by the editor setup. 0 = estimate from height.")]
        public float walkSpeed;
        [Tooltip("Ground speed (m/s) the RUN clip was animated for (measured). 0 = estimate from height.")]
        public float runSpeed;

        /// <summary>
        /// Animation playback speed so the feet match the ground (no foot sliding).
        /// blend = Animator "Speed" value (0.5 walk .. 1 run), groundSpeed = how fast the character really moves.
        /// </summary>
        public float PlaybackSpeed(float blend, float groundSpeed)
        {
            if (groundSpeed < 0.15f) return 1f;
            float walk = walkSpeed > 0.05f ? walkSpeed : height * 1.1f;
            float run = runSpeed > 0.05f ? runSpeed : height * 2.4f;
            float natural = Mathf.Lerp(walk, run, Mathf.InverseLerp(0.5f, 1f, blend));
            float ratio = groundSpeed / Mathf.Max(0.1f, natural);
            // match most of the stride but keep it gentle: fluid motion matters more than perfect feet
            return Mathf.Clamp(Mathf.Lerp(1f, ratio, 0.7f), 0.8f, 2.2f);
        }
    }

    /// <summary>Animator parameter names shared by every generated controller (see Editor/CharacterSetup.cs).</summary>
    public static class AnimParams
    {
        public static readonly int Speed = Animator.StringToHash("Speed");   // 0 idle, 0.5 walk, 1 run
        public static readonly int Win = Animator.StringToHash("Win");       // trigger
        public static readonly int Hit = Animator.StringToHash("Hit");       // trigger
        public static readonly int Die = Animator.StringToHash("Die");       // trigger
        public static readonly int Attack = Animator.StringToHash("Attack"); // trigger
    }

    /// <summary>
    /// Project asset references, created by the editor setup (Gold Rush > Setup Project)
    /// at Assets/GoldRush/Resources/GoldRushAssets.asset and loaded at runtime via Resources.
    /// </summary>
    [CreateAssetMenu(fileName = "GoldRushAssets", menuName = "Gold Rush/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        [Header("Characters (Quaternius, CC0)")]
        public CharacterDef hero = new CharacterDef { height = 1.35f };
        public CharacterDef slime = new CharacterDef { height = 0.95f };       // Patroller
        public CharacterDef ghost = new CharacterDef { height = 1.3f };        // Chaser
        public CharacterDef mushroomKing = new CharacterDef { height = 2.6f }; // Guard

        [Header("Base materials (cloned & tinted at runtime)")]
        public Material lit;            // URP Lit, opaque
        public Material litEmissive;    // URP Lit, opaque, _EMISSION enabled
        public Material litTransparent; // URP Lit, transparent alpha blend
        public Material particle;       // URP Particles/Unlit, transparent alpha blend
        public Material skybox;         // Skybox/Procedural, sunset

        public CharacterDef Enemy(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Chaser: return ghost;
                case EnemyType.Guard: return mushroomKing;
                default: return slime;
            }
        }

        static GameAssets instance;
        public static GameAssets I
        {
            get
            {
                if (instance == null) instance = Resources.Load<GameAssets>("GoldRushAssets");
                if (instance == null) Debug.LogError("[GoldRush] GoldRushAssets missing. Run menu: Gold Rush > Setup Project");
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; }
    }
}
