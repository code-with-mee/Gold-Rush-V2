using UnityEngine;

namespace GoldRush
{
    /// <summary>
    /// A monster starts here. Pick the type in the Inspector:
    /// Patroller = Slime (slow, sees ahead), Chaser = Ghost (fast, sees all around),
    /// Guard = Mushroom King (stays near its Home point).
    /// </summary>
    public class MonsterSpawn : MonoBehaviour
    {
        public EnemyType type = EnemyType.Patroller;
        [Tooltip("Guards stay near this point. Leave empty to use the spawn position.")]
        public Transform home;

        public Vector3 HomePosition => home ? home.position : transform.position;

        void OnDrawGizmos()
        {
            Color c = EnemyAI.SignatureColor(type);
            Gizmos.color = c;
            float h = type == EnemyType.Guard ? 2.6f : type == EnemyType.Chaser ? 1.3f : 0.95f;
            Gizmos.DrawWireCube(transform.position + Vector3.up * h * 0.5f, new Vector3(0.9f, h, 0.9f));
            if (type == EnemyType.Guard)
            {
                Gizmos.color = new Color(c.r, c.g, c.b, 0.35f);
                DrawCircle(HomePosition, 4.5f);
                if (home) Gizmos.DrawLine(transform.position, home.position);
            }
        }

        static void DrawCircle(Vector3 center, float r)
        {
            Vector3 prev = center + new Vector3(r, 0.05f, 0);
            for (int i = 1; i <= 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                Vector3 p = center + new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
