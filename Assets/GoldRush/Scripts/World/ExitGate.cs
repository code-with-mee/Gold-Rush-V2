using UnityEngine;

namespace GoldRush
{
    /// <summary>
    /// Block-style exit gate on the +Z wall: chunky light-grey frame, a sliding door panel that sinks into the floor
    /// when opened, a red light strip that turns green, plus a green floor pad + light once open.
    /// </summary>
    public class ExitGate : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public Vector3 WinPoint => transform.TransformPoint(new Vector3(0f, 0f, -1.5f));
        public Transform MarkerAnchor => markerAnchor;

        // all runtime references are serialized so a gate saved in a scene still opens after loading
        [SerializeField] Transform door, glow, glowRing, markerAnchor;
        [SerializeField] Light glowLight;
        [SerializeField] TextMesh text;
        [SerializeField] Renderer[] strips;
        [SerializeField] Material greenStrip;

        float openT = -1f;   // < 0: door not moving

        static readonly Color Green = new Color(0.3f, 1f, 0.45f);
        static readonly Color Red = new Color(1f, 0.22f, 0.2f);

        const float Opening = 5.5f;   // matches the perimeter wall gap
        const float DoorH = 3.3f;

        public static ExitGate Create(Transform parent, Vector3 pos)
        {
            var go = new GameObject("ExitGate");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var g = go.AddComponent<ExitGate>();
            g.Build();
            return g;
        }

        void Build()
        {
            var t = transform;
            var wall = Props.WallMat;
            var top = Props.WallTopMat;
            var trim = Props.TrimMat;
            var red = Props.Glow(Red, 2.6f);
            greenStrip = Props.Glow(Green, 2.6f);
            float gh = Opening * 0.5f;           // 2.75
            const float zc = 0.5f;               // wall centre line (inner face at z = 0)
            float postX = gh + 0.65f;

            // frame posts + lintel (collide)
            for (int s = -1; s <= 1; s += 2)
            {
                Props.Block(t, new Vector3(s * postX, 0f, zc), new Vector3(1.3f, 4.6f, 1.7f), 0f, wall, top, true);
                // inner trim face of the opening
                var tr = Props.Prim(PrimitiveType.Cube, t, new Vector3(s * (gh - 0.06f), DoorH * 0.5f + 0.1f, zc), new Vector3(0.12f, DoorH + 0.2f, 1.5f), trim, false);
                Mats.NoShadows(tr);
                // base foot
                var f = Props.Prim(PrimitiveType.Cube, t, new Vector3(s * postX, 0.12f, zc - 0.1f), new Vector3(1.6f, 0.24f, 2.0f), trim, false);
                Mats.NoShadows(f);
            }
            Props.Block(t, new Vector3(0f, DoorH + 0.2f, zc), new Vector3(2f * postX + 1.3f, 1.25f, 1.5f), 0f, wall, top, false);
            Props.Prim(PrimitiveType.Cube, t, new Vector3(0f, DoorH + 1.45f + 0.15f, zc), new Vector3(2.2f, 0.3f, 1.0f), wall, false);

            // sign panel on the arena-facing (-Z) side of the lintel
            Props.Prim(PrimitiveType.Cube, t, new Vector3(0f, DoorH + 0.82f, zc - 0.78f), new Vector3(2.6f, 0.9f, 0.08f), trim, false);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var tgo = new GameObject("Text");
            tgo.transform.SetParent(t, false);
            tgo.transform.localPosition = new Vector3(0f, DoorH + 0.83f, zc - 0.84f);
            text = tgo.AddComponent<TextMesh>();
            text.font = font;
            text.text = "EXIT";
            text.fontSize = 100;
            text.characterSize = 0.065f;
            text.fontStyle = FontStyle.Bold;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(1f, 0.38f, 0.34f);
            var trr = tgo.GetComponent<MeshRenderer>();
            if (font) trr.sharedMaterial = font.material;
            trr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // light strips: under the lintel + up both posts (arena side)
            strips = new Renderer[3];
            var sg = Props.Prim(PrimitiveType.Cube, t, new Vector3(0f, DoorH + 0.3f, zc - 0.76f), new Vector3(Opening + 0.2f, 0.12f, 0.06f), red, false);
            strips[0] = sg.GetComponent<Renderer>();
            for (int s = -1; s <= 1; s += 2)
            {
                var v = Props.Prim(PrimitiveType.Cube, t, new Vector3(s * (gh + 0.3f), DoorH * 0.5f, zc - 0.88f), new Vector3(0.12f, DoorH - 0.4f, 0.06f), red, false);
                strips[s < 0 ? 1 : 2] = v.GetComponent<Renderer>();
            }
            foreach (var r in strips) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // sliding door panel (no collider: a blocker behind the opening stops the player)
            door = new GameObject("Door").transform;
            door.SetParent(t, false);
            door.localPosition = Vector3.zero;
            Props.Prim(PrimitiveType.Cube, door, new Vector3(0f, (DoorH + 0.2f) * 0.5f, zc), new Vector3(Opening - 0.2f, DoorH + 0.2f, 0.4f), Props.WallTopMat, false);
            Props.Prim(PrimitiveType.Cube, door, new Vector3(0f, DoorH * 0.5f, zc - 0.22f), new Vector3(Opening - 0.7f, DoorH - 0.6f, 0.06f), wall, false);
            for (int i = 0; i < 3; i++)
                Props.Prim(PrimitiveType.Cube, door, new Vector3(0f, 0.9f + i * 0.75f, zc - 0.26f), new Vector3(Opening - 1.2f, 0.09f, 0.04f), trim, false);
            var hz = Props.Prim(PrimitiveType.Cube, door, new Vector3(0f, 0.3f, zc - 0.23f), new Vector3(Opening - 0.2f, 0.25f, 0.06f), Props.AccentBody(Accent.Orange), false);
            Mats.NoShadows(hz);

            // lanterns on the posts (night only)
            var cap = Props.Glow(Props.LampColor, 2.4f);
            for (int s = -1; s <= 1; s += 2)
            {
                var l = Props.Prim(PrimitiveType.Cube, t, new Vector3(s * postX, 4.6f + 0.18f, zc), new Vector3(0.5f, 0.36f, 0.5f), cap, false);
                Mats.NoShadows(l);
                Props.Prim(PrimitiveType.Cube, t, new Vector3(s * postX, 4.6f + 0.41f, zc), new Vector3(0.66f, 0.1f, 0.66f), top, false);
            }
            var warm = new GameObject("LanternLight").AddComponent<Light>();
            warm.transform.SetParent(t, false);
            warm.transform.localPosition = new Vector3(0f, 3.6f, -1.6f);
            warm.type = LightType.Point;
            warm.color = Props.LampColor;
            warm.range = 8f;
            warm.shadows = LightShadows.None;
            Props.MakeNightLight(warm, 1.8f);

            // green glow (hidden until opened)
            glow = new GameObject("Glow").transform;
            glow.SetParent(t, false);
            glow.localPosition = new Vector3(0f, 0f, -1.6f);
            var disc = Props.Prim(PrimitiveType.Cube, glow, new Vector3(0f, 0.008f, 0f), new Vector3(3.4f, 0.012f, 3.4f), Mats.Transparent(new Color(0.3f, 1f, 0.45f, 0.3f), 0.3f), false);
            Mats.NoShadows(disc);
            var pillar = Props.Prim(PrimitiveType.Cube, glow, new Vector3(0f, 1.6f, 0f), new Vector3(2.8f, 3.2f, 2.8f), Mats.Transparent(new Color(0.4f, 1f, 0.5f, 0.07f), 0.1f), false);
            Mats.NoShadows(pillar);
            Props.FloorPad(glow, Vector3.zero, 3.6f, greenStrip, false);
            glowRing = new GameObject("Ring").transform;
            glowRing.SetParent(glow, false);
            const int segs = 4;
            for (int i = 0; i < segs; i++)
            {
                float a = i * 90f;
                var s = Props.Prim(PrimitiveType.Cube, glowRing, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.02f, 1.1f),
                    new Vector3(1.4f, 0.03f, 0.14f), greenStrip, false, new Vector3(0f, a, 0f));
                Mats.NoShadows(s);
            }
            glow.gameObject.SetActive(false);

            glowLight = new GameObject("GlowLight").AddComponent<Light>();
            glowLight.transform.SetParent(t, false);
            glowLight.transform.localPosition = new Vector3(0f, 2f, -1.5f);
            glowLight.type = LightType.Point;
            glowLight.color = Green;
            glowLight.range = 9f;
            glowLight.intensity = 0f;
            glowLight.shadows = LightShadows.None;
            glowLight.enabled = false;

            markerAnchor = new GameObject("MarkerAnchor").transform;
            markerAnchor.SetParent(t, false);
            markerAnchor.localPosition = new Vector3(0f, 5.6f, 0f);
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (glow) glow.gameObject.SetActive(true);
            if (glowLight) { glowLight.enabled = true; glowLight.intensity = 2.5f; }
            if (text) text.color = new Color(0.55f, 1f, 0.6f);
            if (strips != null)
                foreach (var r in strips) if (r) r.sharedMaterial = greenStrip;
            if (Application.isPlaying && isActiveAndEnabled) openT = 0f;   // slide down in Update
            else if (door) door.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen) return;
            if (openT >= 0f)
            {
                const float dur = 1.2f;
                openT += Time.deltaTime;
                float k = Mathf.Clamp01(openT / dur);
                k = k * k * (3f - 2f * k);
                if (door) door.localPosition = new Vector3(0f, -(DoorH + 0.3f) * k, 0f);
                if (openT >= dur)
                {
                    openT = -1f;
                    if (door) door.gameObject.SetActive(false);
                }
            }
            float t = Time.time;
            float s = 1f + Mathf.Sin(t * 4f) * 0.06f;
            if (glow) glow.localScale = new Vector3(s, 1f, s);
            if (glowRing) glowRing.localRotation = Quaternion.Euler(0f, t * 40f, 0f);
            if (glowLight) glowLight.intensity = 2.5f + Mathf.Sin(t * 4f) * 1f;
        }
    }
}
