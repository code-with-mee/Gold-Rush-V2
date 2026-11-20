using UnityEngine;

namespace GoldRush
{
    /// <summary>Hovering glowing power-up orb with a ground ring. Pickup is distance-based elsewhere (no colliders).</summary>
    public class PowerUp : MonoBehaviour
    {
        public PowerType Type { get; private set; }
        public bool Collected { get; private set; }

        Transform visual, inner, ring;
        float phase;

        public static Color ColorOf(PowerType t)
        {
            switch (t)
            {
                case PowerType.Speed: return new Color(0.2f, 0.9f, 1f);
                case PowerType.Ghost: return new Color(0.76f, 0.56f, 1f);
                default: return new Color(1f, 0.26f, 0.24f);
            }
        }

        public static string NameOf(PowerType t)
        {
            switch (t)
            {
                case PowerType.Speed: return "SPEED";
                case PowerType.Ghost: return "GHOST";
                default: return "MAGNET";
            }
        }

        public static PowerUp Create(Transform parent, Vector3 groundPos, PowerType type)
        {
            var go = new GameObject("PowerUp_" + NameOf(type));
            go.transform.SetParent(parent, false);
            go.transform.position = groundPos;
            var p = go.AddComponent<PowerUp>();
            p.Type = type;
            p.phase = Random.Range(0f, Mathf.PI * 2f);
            p.Build();
            p.Update();
            return p;
        }

        void Build()
        {
            Color c = ColorOf(Type);
            var glow = Mats.Emissive(c, c, 2.2f, 0.6f, 0.1f);
            var shell = Mats.Transparent(new Color(c.r, c.g, c.b, 0.28f), 0.9f);
            var soft = Mats.Transparent(new Color(c.r, c.g, c.b, 0.22f), 0.2f);

            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            visual.localPosition = new Vector3(0f, 1.1f, 0f);

            var orb = Props.Prim(PrimitiveType.Sphere, visual, Vector3.zero, Vector3.one * 0.95f, shell, false);
            Mats.NoShadows(orb);

            inner = new GameObject("Inner").transform;
            inner.SetParent(visual, false);
            switch (Type)
            {
                case PowerType.Speed:
                {
                    var holder = new GameObject("Diamond").transform;
                    holder.SetParent(inner, false);
                    holder.localScale = new Vector3(0.3f, 0.5f, 0.3f);
                    Props.Prim(PrimitiveType.Cube, holder, Vector3.zero, Vector3.one, glow, false, new Vector3(45f, 0f, 35.26f));
                    break;
                }
                case PowerType.Ghost:
                {
                    Props.Prim(PrimitiveType.Sphere, inner, Vector3.zero, Vector3.one * 0.26f, glow, false);
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * Mathf.PI * 2f / 3f;
                        Props.Prim(PrimitiveType.Sphere, inner, new Vector3(Mathf.Cos(a) * 0.24f, (i - 1) * 0.06f, Mathf.Sin(a) * 0.24f),
                            Vector3.one * 0.14f, glow, false);
                    }
                    break;
                }
                default:
                {
                    var tip = Mats.Emissive(new Color(0.95f, 0.95f, 1f), new Color(0.9f, 0.95f, 1f), 1.6f, 0.8f, 0.6f);
                    Props.Prim(PrimitiveType.Cube, inner, new Vector3(0f, -0.15f, 0f), new Vector3(0.42f, 0.11f, 0.11f), glow, false);
                    Props.Prim(PrimitiveType.Cube, inner, new Vector3(-0.155f, 0.02f, 0f), new Vector3(0.11f, 0.3f, 0.11f), glow, false);
                    Props.Prim(PrimitiveType.Cube, inner, new Vector3(0.155f, 0.02f, 0f), new Vector3(0.11f, 0.3f, 0.11f), glow, false);
                    Props.Prim(PrimitiveType.Cube, inner, new Vector3(-0.155f, 0.21f, 0f), new Vector3(0.115f, 0.09f, 0.115f), tip, false);
                    Props.Prim(PrimitiveType.Cube, inner, new Vector3(0.155f, 0.21f, 0f), new Vector3(0.115f, 0.09f, 0.115f), tip, false);
                    break;
                }
            }
            Mats.NoShadows(inner.gameObject);

            // ground ring
            ring = new GameObject("Ring").transform;
            ring.SetParent(transform, false);
            ring.localPosition = new Vector3(0f, 0.03f, 0f);
            var disc = Props.Prim(PrimitiveType.Cylinder, ring, Vector3.zero, new Vector3(1.5f, 0.005f, 1.5f), soft, false);
            Mats.NoShadows(disc);
            const int segs = 12;
            float rad = 0.72f, len = 2f * Mathf.PI * rad / segs * 0.75f;
            for (int i = 0; i < segs; i++)
            {
                float a = i * 360f / segs;
                Vector3 lp = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.01f, rad);
                var s = Props.Prim(PrimitiveType.Cube, ring, lp, new Vector3(len, 0.02f, 0.07f), glow, false, new Vector3(0f, a, 0f));
                Mats.NoShadows(s);
            }
        }

        void Update()
        {
            float t = Time.time;
            if (visual)
            {
                visual.localPosition = new Vector3(0f, 1.1f + Mathf.Sin(t * 2f + phase) * 0.14f, 0f);
                visual.localRotation = Quaternion.Euler(0f, t * 50f, 0f);
            }
            if (inner)
            {
                if (Type == PowerType.Ghost) inner.localRotation = Quaternion.Euler(0f, t * 160f, 0f);
                else inner.localRotation = Quaternion.Euler(0f, t * 90f, Type == PowerType.Magnet ? Mathf.Sin(t * 1.5f) * 12f : 0f);
            }
            if (ring)
            {
                float s = 1f + Mathf.Sin(t * 3f + phase) * 0.06f;
                ring.localScale = new Vector3(s, 1f, s);
                ring.localRotation = Quaternion.Euler(0f, -t * 30f, 0f);
            }
        }

        public void Collect()
        {
            if (Collected) return;
            Collected = true;
            Destroy(gameObject);
        }
    }
}
