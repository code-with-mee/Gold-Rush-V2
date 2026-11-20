using UnityEngine;

namespace GoldRush
{
    /// <summary>Where the hero starts. The blue arrow shows which way the hero (and camera) faces.</summary>
    public class PlayerSpawn : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.25f, 0.6f, 1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.7f, 0.5f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + transform.forward * 2f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.1f + transform.forward * 2f, 0.15f);
        }
    }
}
