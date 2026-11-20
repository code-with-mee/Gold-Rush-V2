using System.Collections.Generic;
using UnityEngine;

namespace GoldRush
{
    /// <summary>Lists materials whose colour changes between day and night (e.g. floor: light grey by day,
    /// navy blue by night). Put one on the level root and edit the colours in the Inspector.</summary>
    public class DayNightTint : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public Material material;
            public Color day = Color.white;
            public Color night = Color.gray;
        }

        public List<Entry> entries = new List<Entry>();

        public void Add(Material m, Color day, Color night)
        {
            if (!m) return;
            foreach (var e in entries) if (e.material == m) { e.day = day; e.night = night; return; }
            entries.Add(new Entry { material = m, day = day, night = night });
        }

        void Start()
        {
            if (!Application.isPlaying) return;
            // Tint runtime COPIES: changing a material asset in Play mode would save the change into the
            // project. Swap the copies onto every renderer in the scene that uses an original.
            var copies = new Dictionary<Material, Material>();
            var all = new List<Entry>(entries);
            var palette = DayNightPalette.Load();
            if (palette) all.AddRange(palette.entries);
            foreach (var e in all)
            {
                if (!e.material || copies.ContainsKey(e.material)) continue;
                var copy = new Material(e.material) { name = e.material.name + " (day/night)" };
                copies[e.material] = copy;
                DayNight.RegisterTint(copy, e.day, e.night);
            }
            foreach (var r in FindObjectsByType<Renderer>())
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] && copies.TryGetValue(mats[i], out var c)) { mats[i] = c; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }
    }
}
