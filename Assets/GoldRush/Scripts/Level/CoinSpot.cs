using UnityEngine;

namespace GoldRush
{
    /// <summary>A gold coin appears here when the level starts. The level is won after collecting all of them.</summary>
    public class CoinSpot : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.78f, 0.2f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 1f, 0.35f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
        }
    }
}
