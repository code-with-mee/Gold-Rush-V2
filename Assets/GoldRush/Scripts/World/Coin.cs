using UnityEngine;

namespace GoldRush
{
    /// <summary>Floating, spinning gold coin. Pickup is distance-based elsewhere (no colliders).</summary>
    public class Coin : MonoBehaviour
    {
        public bool Collected { get; private set; }

        [SerializeField] Transform visual;
        [SerializeField] float phase;

        public static Coin Create(Transform parent, Vector3 groundPos)
        {
            var go = new GameObject("Coin");
            go.transform.SetParent(parent, false);
            go.transform.position = groundPos + Vector3.up;
            var c = go.AddComponent<Coin>();

            var vis = new GameObject("Visual").transform;
            vis.SetParent(go.transform, false);
            c.visual = vis;
            c.phase = Random.Range(0f, Mathf.PI * 2f);

            var gold = Mats.Emissive(new Color(1f, 0.7f, 0.12f), new Color(1f, 0.6f, 0.1f), 0.55f, 0.8f, 0.85f);
            var inner = Mats.Emissive(new Color(0.85f, 0.5f, 0.08f), new Color(0.9f, 0.45f, 0.08f), 0.35f, 0.75f, 0.85f);
            Vector3 upright = new Vector3(90f, 0f, 0f);
            Props.Prim(PrimitiveType.Cylinder, vis, Vector3.zero, new Vector3(0.72f, 0.06f, 0.72f), gold, false, upright);
            var d = Props.Prim(PrimitiveType.Cylinder, vis, Vector3.zero, new Vector3(0.5f, 0.085f, 0.5f), inner, false, upright);
            var star = Props.Prim(PrimitiveType.Cube, vis, Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f), gold, false, new Vector3(0f, 0f, 45f));
            Mats.NoShadows(d);
            Mats.NoShadows(star);
            c.Update();
            return c;
        }

        void Update()
        {
            if (!visual) return;
            float t = Time.time;
            visual.localPosition = new Vector3(0f, Mathf.Sin(t * 2.2f + phase) * 0.12f, 0f);
            visual.rotation = Quaternion.Euler(0f, t * 140f + phase * 57.3f, 0f);
        }

        public void Collect()
        {
            if (Collected) return;
            Collected = true;
            Destroy(gameObject);
        }
    }
}
