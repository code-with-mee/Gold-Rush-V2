using UnityEngine;

namespace GoldRush
{
    /// <summary>Code-built particle effects (sparkles, dust, confetti).</summary>
    public static class FX
    {
        static ParticleSystem Make(string name, Vector3 pos, Texture tex, bool loop, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = loop;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.15f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Particle(tex);
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static void FadeAndShrink(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));
        }

        /// <summary>One-shot burst of sparkles.</summary>
        public static void Burst(Vector3 pos, Color a, Color b, int count = 24, float speedMin = 2f, float speedMax = 5f,
            float size = 0.3f, float life = 0.7f, float gravity = 0.6f, bool stars = true)
        {
            var ps = Make("Burst", pos, stars ? Sprites.StarTex : Sprites.SoftTex, false);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            FadeAndShrink(ps);
            ps.Play();
            ps.Emit(count);
            Object.Destroy(ps.gameObject, life + 0.5f);
        }

        public static void CoinBurst(Vector3 pos)
        {
            Burst(pos, new Color(1f, 0.9f, 0.4f), new Color(1f, 0.65f, 0.1f), 26, 2.5f, 6f, 0.35f, 0.7f, 0.8f, true);
            Burst(pos, new Color(1f, 1f, 0.8f, 0.9f), new Color(1f, 0.85f, 0.4f, 0.6f), 6, 0.2f, 0.6f, 1.4f, 0.35f, 0f, false);
        }

        public static void Puff(Vector3 pos, Color color, int count = 12, float size = 0.8f)
        {
            Burst(pos, color, UIKit.WithAlpha(color, color.a * 0.6f), count, 0.8f, 2.2f, size, 0.8f, -0.05f, false);
        }

        /// <summary>Looping emitter attached to a transform; control with emission.rateOverTime.</summary>
        public static ParticleSystem Trail(Transform parent, Vector3 localPos, Color color, float size, float life, bool stars = false)
        {
            var ps = Make("Trail", parent.position, stars ? Sprites.StarTex : Sprites.SoftTex, true, parent);
            ps.transform.localPosition = localPos;
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(color, UIKit.WithAlpha(color, color.a * 0.5f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.05f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius = 0.25f;
            FadeAndShrink(ps);
            ps.Play();
            return ps;
        }

        public static void Confetti(Vector3 pos)
        {
            var ps = Make("Confetti", pos, Sprites.SquareTex, false);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.gravityModifier = 0.9f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var g = new Gradient();
            g.SetKeys(new[]
                {
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.35f), 0.25f),
                    new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0.5f), new GradientColorKey(new Color(0.4f, 0.95f, 0.5f), 0.75f),
                    new GradientColorKey(new Color(0.8f, 0.5f, 1f), 1f),
                },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 35f;
            sh.radius = 0.3f;
            ps.transform.rotation = Quaternion.Euler(-90, 0, 0);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.75f), new GradientAlphaKey(0, 1) });
            col.color = fade;
            ps.Play();
            ps.Emit(140);
            Object.Destroy(ps.gameObject, 3.5f);
        }
    }
}
