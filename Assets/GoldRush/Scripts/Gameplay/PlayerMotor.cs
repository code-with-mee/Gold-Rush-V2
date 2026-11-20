using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GoldRush
{
    /// <summary>Third-person player: CharacterController movement (collides with props), sprint/stamina,
    /// power-up effects, hit knockback + invulnerability blink, dust, footsteps, win/lose poses.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        public float walkSpeed = 5f;
        public float sprintSpeed = 8.2f;
        public float acceleration = 20f;
        public float turnSpeed = 560f;

        public VirtualJoystick joystick;
        public ThirdPersonCamera cam;

        public float Stamina { get; private set; } = 1f;
        public bool Exhausted { get; private set; }
        public bool Sprinting { get; private set; }
        public bool Frozen { get; set; }
        public bool Invulnerable => Time.time < invulnUntil;
        public Vector3 Velocity => velocity;
        public Animator Anim => anim;

        CharacterController cc;
        float animSpeed = 1f;
        Animator anim;
        Transform model;
        Vector3 velocity, knock;
        float vy, invulnUntil, stepTimer, blinkTimer;
        ParticleSystem dust, boostTrail;
        Renderer[] renderers;
        readonly Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();
        bool ghostVisual;

        public static PlayerMotor Create(Vector3 pos, float yaw)
        {
            var go = new GameObject("Player");
            go.layer = 2; // Ignore Raycast: camera/enemy line-of-sight rays pass through the player
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var cc = go.AddComponent<CharacterController>();
            var def = GameAssets.I.hero;
            cc.height = Mathf.Max(1f, def.height);
            cc.radius = 0.36f;
            cc.center = new Vector3(0, cc.height * 0.5f + 0.02f, 0);
            cc.skinWidth = 0.05f;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            var p = go.AddComponent<PlayerMotor>();
            p.anim = CharacterFactory.BuildModel(def, go.transform);
            p.model = p.anim.transform;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
            Cosmetics.Apply(p.anim, def, Save.Data.color, Save.Data.hat, true);
            p.renderers = go.GetComponentsInChildren<Renderer>();
            p.dust = FX.Trail(go.transform, new Vector3(0, 0.1f, -0.2f), new Color(0.9f, 0.78f, 0.6f, 0.55f), 0.7f, 0.7f);
            p.boostTrail = FX.Trail(go.transform, new Vector3(0, 0.9f, -0.2f), new Color(0.4f, 0.95f, 1f, 0.8f), 0.35f, 0.5f, true);
            return p;
        }

        void Awake() { cc = GetComponent<CharacterController>(); }

        Vector2 ReadInput()
        {
            Vector2 v = joystick ? joystick.Value : Vector2.zero;
            float kx = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            float ky = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            if (kx != 0 || ky != 0) v = new Vector2(kx, ky).normalized;
            return Vector2.ClampMagnitude(v, 1f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var gm = GameManager.I;
            bool canMove = !Frozen && gm != null && gm.State == GameState.Playing;

            Vector2 input = canMove ? ReadInput() : Vector2.zero;
            float yaw = cam ? cam.Yaw : 0f;
            Vector3 dir = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
            float mag = Mathf.Clamp01(dir.magnitude);

            bool wantSprint = canMove && mag > 0.2f && ((joystick && joystick.Held && joystick.Value.magnitude > 0.9f) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            if (Exhausted && Stamina > 0.35f) Exhausted = false;
            Sprinting = wantSprint && !Exhausted && Stamina > 0f;
            if (Sprinting)
            {
                Stamina = Mathf.Max(0f, Stamina - dt * 0.3f);
                if (Stamina <= 0f) Exhausted = true;
            }
            else Stamina = Mathf.Min(1f, Stamina + dt * (mag > 0.1f ? 0.18f : 0.32f));

            float mul = gm ? gm.SpeedMultiplier : 1f;
            float speed = (Sprinting ? sprintSpeed : walkSpeed) * mul;
            Vector3 targetVel = mag > 0.01f ? dir.normalized * (mag * speed) : Vector3.zero;
            velocity = Vector3.MoveTowards(velocity, targetVel, acceleration * dt);

            if (velocity.sqrMagnitude > 0.05f && !Frozen)
            {
                var targetRot = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * dt);
            }

            if (cc.isGrounded && vy < 0) vy = -2f; else vy -= 25f * dt;
            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-5f * dt));
            if (cc.enabled) cc.Move((velocity + knock + Vector3.up * vy) * dt);

            // animation (0 idle, 0.5 walk, 1 run)
            float horiz = new Vector3(velocity.x, 0, velocity.z).magnitude;
            float blend = horiz < 0.1f ? 0f : Mathf.Lerp(0.45f, 1f, Mathf.InverseLerp(2.5f, sprintSpeed, horiz));
            if (anim && anim.runtimeAnimatorController) anim.SetFloat(AnimParams.Speed, blend, 0.15f, dt);
            // play the walk/run cycle exactly as fast as the hero moves, so the feet don't slide
            if (anim && !Frozen)
            {
                float target = GameAssets.I.hero.PlaybackSpeed(blend, horiz);
                animSpeed = Mathf.Lerp(animSpeed, target, 1f - Mathf.Exp(-5f * dt)); // ease, never jump
                anim.speed = animSpeed;
            }

            // footsteps
            if (horiz > 1f && cc.isGrounded)
            {
                stepTimer -= dt * horiz / 5f;
                if (stepTimer <= 0f)
                {
                    stepTimer = 0.36f;
                    AudioFX.Play("step", Sprinting ? 0.55f : 0.35f, Random.Range(0.9f, 1.1f));
                }
            }

            // dust + boost trail
            if (dust) { var em = dust.emission; em.rateOverTime = horiz > 1f ? (Sprinting ? 28f : 9f) : 0f; }
            if (boostTrail) { var em = boostTrail.emission; em.rateOverTime = gm && gm.SpeedActive && horiz > 0.5f ? 35f : 0f; }

            // invulnerability blink
            if (Invulnerable)
            {
                blinkTimer -= dt;
                if (blinkTimer <= 0f) { blinkTimer = 0.09f; SetVisible(!renderers[0].enabled); }
            }
            else if (renderers != null && renderers.Length > 0 && !renderers[0].enabled) SetVisible(true);

            bool ghost = gm && gm.GhostActive;
            if (ghost != ghostVisual) SetGhost(ghost);
        }

        void SetVisible(bool v)
        {
            if (renderers == null) return;
            foreach (var r in renderers) if (r && !(r is ParticleSystemRenderer)) r.enabled = v;
        }

        void SetGhost(bool on)
        {
            ghostVisual = on;
            var ghostMat = Mats.Transparent(new Color(0.65f, 0.6f, 1f, 0.32f), 0.9f);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (on)
                {
                    if (!originalMats.ContainsKey(r)) originalMats[r] = r.sharedMaterials;
                    var arr = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < arr.Length; i++) arr[i] = ghostMat;
                    r.sharedMaterials = arr;
                }
                else if (originalMats.TryGetValue(r, out var orig)) r.sharedMaterials = orig;
            }
            if (!on) originalMats.Clear();
        }

        public void Hit(Vector3 from, float invulnSeconds)
        {
            Vector3 d = transform.position - from; d.y = 0;
            if (d.sqrMagnitude < 0.01f) d = -transform.forward;
            knock = d.normalized * 13f;
            invulnUntil = Time.time + invulnSeconds;
            if (anim) anim.SetTrigger(AnimParams.Hit);
            FX.Puff(transform.position + Vector3.up * 1f, new Color(1f, 0.9f, 0.8f, 0.8f), 14, 0.9f);
        }

        public void PlayVictory()
        {
            Frozen = true;
            velocity = Vector3.zero;
            StartCoroutine(VictoryCo());
        }

        IEnumerator VictoryCo()
        {
            if (anim) anim.SetFloat(AnimParams.Speed, 0f);
            // turn to face the camera, then wave (the "Win" clip)
            Quaternion start = transform.rotation;
            Vector3 toCam = cam ? cam.transform.position - transform.position : -transform.forward;
            toCam.y = 0;
            Quaternion face = toCam.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCam) : start;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.35f)
            {
                transform.rotation = Quaternion.Slerp(start, face, t);
                yield return null;
            }
            if (anim) { anim.speed = 1f; anim.SetTrigger(AnimParams.Win); }
        }

        public void PlayCaught()
        {
            Frozen = true;
            velocity = Vector3.zero;
            StartCoroutine(CaughtCo());
        }

        IEnumerator CaughtCo()
        {
            SetVisible(true);
            if (anim) { anim.speed = 1f; anim.SetFloat(AnimParams.Speed, 0f); anim.SetTrigger(AnimParams.Die); }
            yield return new WaitForSeconds(0.5f);
            FX.Puff(transform.position + Vector3.up * 0.2f, new Color(0.9f, 0.78f, 0.6f, 0.8f), 18, 1.1f);
        }

        /// <summary>A warm lantern spotlight follows the hero (and a soft fill light). Both fade in at night.</summary>
        public void AddLantern()
        {
            var spot = new GameObject("Lantern").AddComponent<Light>();
            spot.transform.SetParent(transform, false);
            spot.transform.localPosition = new Vector3(0f, 5.5f, -1.5f);
            spot.transform.LookAt(transform.position + transform.forward * 1.8f);
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.8f, 0.5f);
            spot.intensity = 22f;
            spot.range = 14f;
            spot.spotAngle = 62f;
            spot.innerSpotAngle = 30f;
            spot.shadows = LightShadows.Soft;
            DayNight.RegisterNightLight(spot);
            var fill = CharacterFactory.AddGlow(transform, new Color(1f, 0.85f, 0.6f), 1.35f, 0.5f);
            fill.transform.localPosition = new Vector3(0f, 2.4f, -1.2f); // above & behind: soft rim light, not a face flash
            fill.range = 4f;
            DayNight.RegisterNightLight(fill);
        }
    }
}
