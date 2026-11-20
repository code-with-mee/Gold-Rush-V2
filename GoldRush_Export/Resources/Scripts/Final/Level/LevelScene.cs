using UnityEngine;

namespace GoldRush
{
    /// <summary>
    /// Put this on the root of a hand-made level. The GameManager finds it and uses the marker objects
    /// below it (PlayerSpawn, CoinSpot, MonsterSpawn, PowerUpSpot, ExitGate) instead of generating a level.
    /// </summary>
    public class LevelScene : MonoBehaviour
    {
        [Tooltip("Half the width of the playable square (monsters wander inside ±this).")]
        public float arenaHalf = 16f;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, new Vector3(arenaHalf * 2, 0.1f, arenaHalf * 2));
        }
    }
}
