using UnityEngine;

namespace GoldRush
{
    /// <summary>Smooth orbit camera: touch-drag / right-mouse / Q-E to rotate, wall collision, shake, intro swoop.
    /// Rotation is frame-rate independent (fixes the old per-frame joystick rotation).</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 6.2f;
        public float height = 1.15f;
        public float minPitch = 4f, maxPitch = 62f;
        public float touchSensitivity = 0.16f; // degrees per reference pixel (1920 wide)
        [Tooltip("Mouse look speed on computers (cursor is locked while playing).")]
        public float mouseSensitivity = 2.4f;
        public float yaw, pitch = 20f;

        /// <summary>Smoothed yaw the camera is actually using (movement is relative to this).</summary>
        public float Yaw => curYaw;

        Vector3 focus, focusVel;
        float curDist, distVel, targetDist;
        float curYaw, curPitch, yawVel, pitchVel;
        float shakeAmp, shakeTime, shakeDur;
        bool intro;
        float introT, introDur;

        public static ThirdPersonCamera Create(Transform target, float yaw)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            go.AddComponent<AudioListener>();
            var c = go.AddComponent<ThirdPersonCamera>();
            c.Init(target, yaw);
            return c;
        }

        /// <summary>Starts following a target from behind (also used for a camera placed in the scene).</summary>
        public void Init(Transform newTarget, float startYaw)
        {
            target = newTarget;
            yaw = startYaw;
            focus = target.position + Vector3.up * height;
            curDist = targetDist = distance;
            curYaw = yaw;
            curPitch = pitch;
            Snap();
        }

        void Start()
        {
            // a camera placed in the scene with a Target set, that nobody initialised yet
            if (target && curDist <= 0f) Init(target, target.eulerAngles.y);
        }

        public void PlayIntro(float duration)
        {
            intro = true;
            introT = 0;
            introDur = duration;
            curDist = distance * 4f;
        }

        public void Shake(float amplitude, float duration)
        {
            shakeAmp = Mathf.Max(shakeAmp * (shakeTime / Mathf.Max(0.01f, shakeDur)), amplitude);
            shakeDur = shakeTime = duration;
        }

        void Snap()
        {
            var rot = Quaternion.Euler(curPitch, curYaw, 0);
            transform.SetPositionAndRotation(focus + rot * Vector3.back * curDist, rot);
        }

        void LateUpdate()
        {
            if (!target) return;
            float dt = Time.deltaTime;

            // ---- input changes the TARGET angles (yaw / pitch); the camera eases toward them
            if (Time.timeScale > 0f)
            {
                Vector2 look = TouchLookArea.Consume() * (1920f / Mathf.Max(1, Screen.width));
                yaw += look.x * touchSensitivity;
                pitch -= look.y * touchSensitivity * 0.8f;
                // computer: mouse look while the cursor is locked (like a PC 3D game);
                // right-drag still works when the cursor is free
                if (Cursor.lockState == CursorLockMode.Locked || Input.GetMouseButton(1))
                {
                    yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                    pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * 0.8f;
                }
                if (Input.GetKey(KeyCode.Q)) yaw -= 110f * dt;
                if (Input.GetKey(KeyCode.E)) yaw += 110f * dt;
                distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 0.5f, 4f, 11f);
            }
            else TouchLookArea.Consume();
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            if (dt > 0f)
            {
                curYaw = Mathf.SmoothDampAngle(curYaw, yaw, ref yawVel, 0.09f, Mathf.Infinity, dt);
                curPitch = Mathf.SmoothDamp(curPitch, pitch, ref pitchVel, 0.09f, Mathf.Infinity, dt);
            }

            // ---- follow point: slightly behind the hero, leading a little in the running direction
            Vector3 lead = Vector3.zero;
            var motor = target.GetComponent<PlayerMotor>();
            if (motor) { lead = motor.Velocity * 0.12f; lead.y = 0f; }
            Vector3 want = target.position + Vector3.up * height + lead;
            if (dt > 0f) focus = Vector3.SmoothDamp(focus, want, ref focusVel, 0.12f, Mathf.Infinity, dt);

            float p = curPitch, d = distance;
            if (intro)
            {
                introT += dt;
                float k = Tween.InOutSine(Mathf.Clamp01(introT / introDur));
                p = Mathf.Lerp(58f, curPitch, k);
                d = Mathf.Lerp(distance * 4f, distance, k);
                curDist = targetDist = d;
                if (introT >= introDur) intro = false;
            }

            Quaternion rot = Quaternion.Euler(p, curYaw, 0);
            Vector3 dir = rot * Vector3.back;

            // ---- wall avoidance: several rays (centre + 4 corners of the view), take the nearest hit.
            // Small changes are ignored (no flicker) and the camera glides in/out instead of snapping.
            if (!intro)
            {
                float allowed = d;
                Vector3 right = rot * Vector3.right, up = rot * Vector3.up;
                if (Physics.SphereCast(focus, 0.22f, dir, out var hit, d, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    allowed = Mathf.Min(allowed, hit.distance);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 off = (i < 2 ? right : up) * (i % 2 == 0 ? 0.35f : -0.35f);
                    if (Physics.Raycast(focus + off, dir, out var h2, d, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        allowed = Mathf.Min(allowed, h2.distance);
                }
                // also make sure nothing hides the hero's body (low walls between hero and camera)
                Vector3 camGuess = focus + dir * d;
                Vector3 body = target.position + Vector3.up * 0.6f;
                Vector3 toCam = camGuess - body;
                if (Physics.Raycast(body, toCam.normalized, out var h3, toCam.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    // distance along the camera ray that corresponds to the blocking point
                    float frac = h3.distance / Mathf.Max(0.01f, toCam.magnitude);
                    allowed = Mathf.Min(allowed, d * frac);
                }
                allowed = Mathf.Max(1.4f, allowed - 0.25f);
                if (allowed < targetDist - 0.05f || allowed > targetDist + 0.35f) targetDist = allowed; // hysteresis
                bool pullingIn = targetDist < curDist;
                if (dt > 0f) curDist = Mathf.SmoothDamp(curDist, targetDist, ref distVel, pullingIn ? 0.1f : 0.45f, Mathf.Infinity, dt);
                curDist = Mathf.Min(curDist, allowed + 0.6f); // never sink deep inside a wall
            }

            Vector3 pos = focus + dir * curDist;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(shakeTime / shakeDur);
                float t = Time.unscaledTime * 40f;
                pos += (transform.right * (Mathf.PerlinNoise(t, 0.3f) - 0.5f) + transform.up * (Mathf.PerlinNoise(0.7f, t) - 0.5f)) * (2f * shakeAmp * k);
            }
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
