using UnityEngine;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>
    /// Connects a menu Button to an action by NAME, so it keeps working when the menu is saved in the scene
    /// (click listeners added from code are not saved). Change "action" in the Inspector to rewire a button:
    /// play, levels, shop, back, settings.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuAction : MonoBehaviour
    {
        public string action = "play";

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioFX.Play("click");
                if (GoldRushMenu.I) GoldRushMenu.I.Run(action);
            });
        }
    }
}
