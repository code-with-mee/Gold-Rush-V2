using System.Collections.Generic;
using UnityEngine;

namespace GoldRush
{
    /// <summary>
    /// Project-wide list of block materials and their day/night colours
    /// (Assets/GoldRush/Resources/DayNightPalette.asset). Every DayNightTint also applies these,
    /// so prefabs dragged into a level change colour at night automatically.
    /// </summary>
    public class DayNightPalette : ScriptableObject
    {
        public List<DayNightTint.Entry> entries = new List<DayNightTint.Entry>();

        public void Add(Material m, Color day, Color night)
        {
            if (!m) return;
            foreach (var e in entries) if (e.material == m) { e.day = day; e.night = night; return; }
            entries.Add(new DayNightTint.Entry { material = m, day = day, night = night });
        }

        public static DayNightPalette Load() => Resources.Load<DayNightPalette>("DayNightPalette");
    }
}
