using UnityEngine;

namespace GoldRush
{
    /// <summary>A power-up appears here: SPEED (faster), GHOST (monsters can't see you) or MAGNET (pulls coins).</summary>
    public class PowerUpSpot : MonoBehaviour
    {
        public PowerType type = PowerType.Speed;

        void OnDrawGizmos()
        {
            Color c = type == PowerType.Speed ? new Color(0.3f, 0.9f, 1f) : type == PowerType.Ghost ? new Color(0.7f, 0.55f, 1f) : new Color(1f, 0.3f, 0.3f);
            Gizmos.color = c;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.1f, 0.45f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.65f);
        }
    }
}
