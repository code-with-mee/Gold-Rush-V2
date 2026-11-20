using UnityEngine;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>
    /// Put this on a Button in the game UI and type its action: "pause", "resume", "retry", "menu" or "next".
    /// When clicked it plays the click sound and calls GameManager.Run(action).
    /// (Like MenuAction for the main menu.)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GameAction : MonoBehaviour
    {
        public string action = "resume";

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioFX.Play("click");
                if (GameManager.I) GameManager.I.Run(action);
            });
        }
    }
}
