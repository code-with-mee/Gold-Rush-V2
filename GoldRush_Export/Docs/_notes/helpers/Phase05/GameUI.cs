using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>
    /// In-game HUD + modal screens, all built in code.
    /// PHASE 5 VERSION: coin counter + message toast only.
    /// The full GameUI.cs replaces this file in a later phase.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        GameManager gm;
        RectTransform canvasRt, hud, modal;

        Text coinText; RectTransform coinPill, coinIcon;
        CanvasGroup toastGroup; RectTransform toastRt; Text toastTitle, toastSub; Image toastBg, toastLip, toastShadow; Coroutine toastCo;

        public static GameUI Create(GameManager gm)
        {
            var canvas = UIKit.Canvas("GameUI", 10);
            var ui = canvas.gameObject.AddComponent<GameUI>();
            ui.gm = gm;
            ui.canvasRt = (RectTransform)canvas.transform;
            ui.Build();
            return ui;
        }

        // ================================================================ HUD

        void Build()
        {
            hud = UIKit.Rect("HUD", canvasRt); hud.Fill();

            // ---- compact frosted-glass HUD: small chips in the corners so the map stays visible
            // coin counter (top-left)
            coinPill = UIKit.Rect("CoinPill", hud).At(new Vector2(0, 1), new Vector2(32, -28), new Vector2(214, 70));
            UIKit.Glass(coinPill, Sprites.Pill, 128f / 70f, 0.5f);
            coinIcon = UIKit.CoinIcon(coinPill, 54);
            coinIcon.At(new Vector2(0, 0.5f), new Vector2(38, 0), new Vector2(54, 54), new Vector2(0.5f, 0.5f)); // centred pivot so the flip spins in place
            coinText = UIKit.Label(coinPill, "0 / 0", 38, UIKit.Cream, TextAnchor.MiddleLeft);
            coinText.rectTransform.Fill(76, 0, 8, 0);

            toastRt = UIKit.Rect("Toast", hud).At(new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(640, 96), new Vector2(0.5f, 0.5f));
            toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0; toastGroup.blocksRaycasts = false;
            toastShadow = UIKit.Img(toastRt, Sprites.Pill, new Color(0, 0, 0, 0.18f), "Shadow"); toastShadow.rectTransform.Fill(2, -6, 2, 4); toastShadow.pixelsPerUnitMultiplier = 128f / 96f;
            toastBg = UIKit.Glass(toastRt, Sprites.Pill, 128f / 96f, 0.55f); // same glass as the HUD chips
            toastLip = toastBg;
            toastTitle = UIKit.Label(toastRt, "", 38, UIKit.Cream); toastTitle.rectTransform.Fill(0, 34, 0, 0);
            toastSub = UIKit.Label(toastRt, "", 22, UIKit.Cream); toastSub.rectTransform.Fill(0, 8, 0, 60);

            coinIcon.gameObject.AddComponent<CoinFlip>();
            PlayHudIntro();

            modal = UIKit.Rect("Modal", canvasRt); modal.Fill();
        }

        /// <summary>HUD pieces drop in from above the screen, one after another.</summary>
        void PlayHudIntro()
        {
            var items = new[] { coinPill };
            for (int i = 0; i < items.Length; i++)
            {
                var rt = items[i];
                if (!rt) continue;
                Vector2 target = rt.anchoredPosition;
                rt.anchoredPosition = target + Vector2.up * 220f;
                Tween.Move(rt, target, 0.55f, Tween.OutBack, 0.25f + i * 0.08f);
            }
        }

        public void SetCoins(int got, int total, bool punch)
        {
            coinText.text = $"{got} <size=40><color=#BFD3F2>/ {total}</color></size>";
            if (punch)
            {
                Tween.Punch(coinPill, 0.18f, 0.3f);
                Tween.Punch(coinIcon, 0.45f, 0.35f);
            }
        }

        public void FlyCoin(Vector3 world, int got, int total)
        {
            var cam = Camera.main;
            if (!cam) { SetCoins(got, total, true); return; }
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0) { SetCoins(got, total, true); return; }
            var icon = UIKit.CoinIcon(hud, 70, "FlyCoin");
            icon.anchorMin = icon.anchorMax = Vector2.zero;
            icon.position = new Vector3(sp.x, sp.y, 0);
            Vector3 from = icon.position;
            Tween.Run(icon, 0.55f, k =>
            {
                Vector3 to = coinIcon.position;
                Vector3 mid = Vector3.Lerp(from, to, 0.5f) + Vector3.up * Screen.height * 0.12f;
                Vector3 a = Vector3.Lerp(from, mid, k), b = Vector3.Lerp(mid, to, k);
                icon.position = Vector3.Lerp(a, b, k);
                icon.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.8f, k);
            }, Tween.InCubic, 0f, () =>
            {
                Destroy(icon.gameObject);
                SetCoins(got, total, true);
            });
        }

        public void Toast(string title, string sub, Color color)
        {
            toastTitle.text = title;
            toastSub.text = sub;
            toastTitle.color = Color.Lerp(color, Color.white, 0.25f); // message colour shows in the title
            if (toastCo != null) StopCoroutine(toastCo);
            toastCo = StartCoroutine(ToastCo());
        }

        IEnumerator ToastCo()
        {
            toastGroup.alpha = 1;
            // 3D flip-in: rotate down from "lying flat" while popping up
            Tween.Run(toastRt, 0.45f, k =>
            {
                toastRt.localRotation = Quaternion.Euler(Mathf.LerpUnclamped(80f, 0f, k), 0, 0);
                toastRt.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, k);
            }, Tween.OutBack);
            yield return new WaitForSecondsRealtime(2.2f);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.35f)
            {
                toastGroup.alpha = 1 - t;
                yield return null;
            }
            toastGroup.alpha = 0;
        }
    }
}
