using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>Monster AI with three personalities (Slime, Ghost, Mushroom King):
    /// Patroller – slow wanderer, narrow vision cone. Chaser – fast, sees all around. Guard – circles a post and leashes back.
    /// States: Patrol → Alert ("!") → Chase → Search ("?") → Patrol; Stunned ("zZ") after catching the player.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        enum S { Patrol, Alert, Chase, Search, Return, Stunned }

        public EnemyType Type { get; private set; }
        public bool IsChasing => state == S.Alert || state == S.Chase;

        NavMeshAgent agent;
        Animator anim;
        S state;
        float stateTime;
        Vector3 home, lastSeen, patrolPoint;
        bool hasPatrolPoint;
        float nextRepath, nextVision, lastSeenTime, waitUntil, searchEnd = -1f;
        bool canSee;

        // tuning
        float patrolSpeed, chaseSpeed, detectRange, loseRange, fovCos, patrolRadius, leash, animMax;
        float speedMul = 1f;
        float smoothSpeed, animSpeed = 1f;
        float height = 1.4f, catchRadius = 1.05f;
        bool celebrated;
        float half;

        // UI bubble
        WorldMarkers.Marker marker;
        Image bubbleBg;
        Text bubbleText;
        string bubbleMode = "";

        public static EnemyAI Create(Transform parent, Vector3 pos, EnemyType type, Vector3 home, float speedMul, float arenaHalf, WorldMarkers markers)
        {
            var go = new GameObject("Monster_" + type);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.acceleration = 22f;
            agent.angularSpeed = 720f;
            agent.stoppingDistance = 0.2f;
            agent.autoBraking = true;
            agent.updateRotation = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(30, 70);
            agent.Warp(pos);

            var e = go.AddComponent<EnemyAI>();
            e.agent = agent;
            e.Type = type;
            e.home = home;
            e.speedMul = speedMul;
            e.half = arenaHalf;
            var def = GameAssets.I.Enemy(type);
            e.height = def.height;
            agent.height = def.height;
            agent.radius = type == EnemyType.Guard ? 0.75f : 0.45f;
            e.catchRadius = type == EnemyType.Guard ? 1.6f : 1.05f;
            e.anim = CharacterFactory.BuildModel(def, go.transform);
            e.Configure();
            e.Decorate();
            if (markers) e.BuildBubble(markers);
            return e;
        }

        void Configure()
        {
            switch (Type)
            {
                case EnemyType.Patroller:
                    patrolSpeed = 1.5f; chaseSpeed = 3.6f; detectRange = 8.5f; loseRange = 14f; fovCos = Mathf.Cos(65f * Mathf.Deg2Rad); patrolRadius = 9f; leash = 999f; break;
                case EnemyType.Chaser:
                    patrolSpeed = 2.1f; chaseSpeed = 4.6f; detectRange = 12f; loseRange = 20f; fovCos = -1f; patrolRadius = 14f; leash = 999f; break;
                default: // Guard
                    patrolSpeed = 1.2f; chaseSpeed = 4.2f; detectRange = 9.5f; loseRange = 14f; fovCos = Mathf.Cos(100f * Mathf.Deg2Rad); patrolRadius = 4.5f; leash = 13f; break;
            }
            patrolSpeed *= speedMul;
            chaseSpeed *= speedMul;
            animMax = chaseSpeed;
            agent.speed = patrolSpeed;
            state = S.Patrol;
        }

        /// <summary>Each enemy type has a signature colour; at night it glows in that colour
        /// (slime = toxic green, ghost = pink, mushroom king = gold) so players can read it in the dark.</summary>
        public static Color SignatureColor(EnemyType t)
        {
            switch (t)
            {
                case EnemyType.Chaser: return new Color(1f, 0.3f, 0.8f);
                case EnemyType.Guard: return new Color(1f, 0.8f, 0.25f);
                default: return new Color(0.25f, 1f, 0.6f);
            }
        }

        /// <summary>How high the ITSTEP name patch sits on each monster's body (0 feet .. 1 top).</summary>
        public static float BadgeHeight(EnemyType t) => t == EnemyType.Guard ? 0.36f : t == EnemyType.Chaser ? 0.5f : 0.42f;

        void Decorate()
        {
            Badge.Attach(anim, GameAssets.I.Enemy(Type), BadgeHeight(Type), SignatureColor(Type));
            var glow = CharacterFactory.AddGlow(transform, SignatureColor(Type), height, Type == EnemyType.Guard ? 3f : 2.2f);
            DayNight.RegisterNightLight(glow); // glows only at night
        }

        void BuildBubble(WorldMarkers markers)
        {
            var root = UIKit.Rect("Bubble", markers.transform);
            root.sizeDelta = new Vector2(56, 56);
            var sh = UIKit.Img(root, Sprites.Circle, UIKit.Shadow, "Shadow"); sh.rectTransform.Fill(-2, -6, -2, 2);
            bubbleBg = UIKit.Img(root, Sprites.Circle, UIKit.Red, "Bg"); bubbleBg.rectTransform.Fill();
            bubbleText = UIKit.Label(root, "!", 40, UIKit.Cream); bubbleText.rectTransform.Fill(0, 2, 0, 0);
            marker = markers.Add(transform, Vector3.up * (height + 0.6f), root, 45f);
            marker.visible = false;
        }

        void SetBubble(string mode)
        {
            if (marker == null || mode == bubbleMode) return;
            bubbleMode = mode;
            marker.visible = mode != "";
            switch (mode)
            {
                case "alert": bubbleBg.color = UIKit.Red; bubbleText.text = "!"; break;
                case "search": bubbleBg.color = UIKit.Gold; bubbleText.text = "?"; break;
                case "stun": bubbleBg.color = UIKit.Blue; bubbleText.text = "z"; break;
            }
            if (mode != "") Tween.PopIn(marker.rt, 0.3f);
        }

        void Go(S s)
        {
            state = s;
            stateTime = 0f;
            switch (s)
            {
                case S.Patrol: agent.isStopped = false; agent.speed = patrolSpeed; hasPatrolPoint = false; SetBubble(""); break;
                case S.Alert: agent.isStopped = true; SetBubble("alert"); AudioFX.Play("alert", 0.8f, Random.Range(0.95f, 1.08f)); break;
                case S.Chase: agent.isStopped = false; agent.speed = chaseSpeed; break;
                case S.Search: searchEnd = -1f; agent.isStopped = false; agent.speed = patrolSpeed * 1.4f; agent.SetDestination(lastSeen); SetBubble("search"); break;
                case S.Return: agent.isStopped = false; agent.speed = patrolSpeed * 1.3f; agent.SetDestination(home); SetBubble(""); break;
                case S.Stunned: agent.isStopped = true; agent.velocity = Vector3.zero; SetBubble("stun"); break;
            }
        }

        public void Stun(float seconds)
        {
            if (anim) anim.SetTrigger(AnimParams.Attack);
            Go(S.Stunned);
            waitUntil = Time.time + seconds;
        }

        public void Enrage(float mul)
        {
            chaseSpeed *= mul; patrolSpeed *= mul; detectRange *= 1.15f;
            animMax = chaseSpeed;
            agent.speed = state == S.Chase ? chaseSpeed : patrolSpeed;
        }

        bool Vision(PlayerMotor p, GameManager gm)
        {
            if (gm.GhostActive) return false;
            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 target = p.transform.position + Vector3.up * 1.1f;
            Vector3 to = target - eye;
            float dist = to.magnitude;
            float range = state == S.Chase || state == S.Search ? loseRange : detectRange;
            if (dist > range) return false;
            Vector3 flat = new Vector3(to.x, 0, to.z).normalized;
            bool close = dist < 3.2f;
            if (!close && state != S.Chase && Vector3.Dot(transform.forward, flat) < fovCos) return false;
            if (Physics.Raycast(eye, to / dist, dist - 0.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (!gm || !agent || !agent.isOnNavMesh) return;
            float dt = Time.deltaTime;
            stateTime += dt;

            if (gm.State == GameState.Won || gm.State == GameState.Lost)
            {
                if (!agent.isStopped) { agent.isStopped = true; agent.velocity = Vector3.zero; }
                SetBubble("");
                // the player got caught: monsters celebrate with their win animation
                if (gm.State == GameState.Lost && !celebrated && anim) { celebrated = true; anim.SetTrigger(AnimParams.Win); }
                Animate(dt);
                return;
            }

            var player = gm.Player;
            bool playing = gm.State == GameState.Playing && player;

            if (playing && Time.time >= nextVision)
            {
                nextVision = Time.time + 0.15f;
                canSee = Vision(player, gm);
                if (canSee) { lastSeen = player.transform.position; lastSeenTime = Time.time; }
            }
            if (!playing) canSee = false;

            switch (state)
            {
                case S.Patrol:
                    Wander();
                    if (canSee) Go(S.Alert);
                    break;

                case S.Alert:
                    FaceTowards(lastSeen, dt);
                    if (stateTime > 0.45f) Go(S.Chase);
                    break;

                case S.Chase:
                    if (Time.time >= nextRepath && playing)
                    {
                        nextRepath = Time.time + 0.12f;
                        agent.SetDestination(canSee ? player.transform.position : lastSeen);
                    }
                    if (stateTime > 1.2f && bubbleMode == "alert") SetBubble("");
                    if (gm.GhostActive || (!canSee && Time.time - lastSeenTime > 2.2f)) Go(S.Search);
                    else if (Type == EnemyType.Guard && (transform.position - home).sqrMagnitude > leash * leash) Go(S.Return);
                    break;

                case S.Search:
                    if (canSee) { Go(S.Alert); break; }
                    if (!agent.pathPending && agent.remainingDistance <= 0.6f)
                    {
                        transform.Rotate(0, 140f * dt, 0);
                        if (searchEnd < 0) searchEnd = Time.time + 2.2f;
                        else if (Time.time > searchEnd) Go(Type == EnemyType.Guard ? S.Return : S.Patrol);
                    }
                    if (stateTime > 9f) Go(S.Patrol);
                    break;

                case S.Return:
                    if (canSee && (player.transform.position - home).sqrMagnitude < leash * leash) { Go(S.Alert); break; }
                    if (!agent.pathPending && agent.remainingDistance <= 1f) Go(S.Patrol);
                    break;

                case S.Stunned:
                    transform.Rotate(0, 200f * dt, 0);
                    if (Time.time > waitUntil) Go(S.Search);
                    break;
            }

            // catch check (distance-based; enemies have no colliders)
            if (playing && state != S.Stunned && state != S.Alert)
            {
                Vector3 d = player.transform.position - transform.position;
                if (Mathf.Abs(d.y) < 1.5f && d.x * d.x + d.z * d.z < catchRadius * catchRadius) gm.OnPlayerCaught(this);
            }

            Animate(dt);
        }

        void Wander()
        {
            bool arrived = !agent.pathPending && agent.remainingDistance <= 0.5f;
            if (arrived && hasPatrolPoint && waitUntil <= 0) waitUntil = Time.time + Random.Range(0.8f, 2.4f);
            bool waitedEnough = arrived && waitUntil > 0 && Time.time > waitUntil;
            if (!hasPatrolPoint || waitedEnough || stateTime > 12f)
            {
                Vector3 center = Type == EnemyType.Guard ? home : transform.position;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 c = Random.insideUnitCircle * patrolRadius;
                    Vector3 p = center + new Vector3(c.x, 0, c.y);
                    p.x = Mathf.Clamp(p.x, -half + 1.5f, half - 1.5f);
                    p.z = Mathf.Clamp(p.z, -half + 1.5f, half - 1.5f);
                    if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas))
                    {
                        patrolPoint = hit.position;
                        agent.SetDestination(patrolPoint);
                        hasPatrolPoint = true;
                        break;
                    }
                }
                waitUntil = 0;
                stateTime = 0;
            }
        }

        void FaceTowards(Vector3 p, float dt)
        {
            Vector3 d = p - transform.position; d.y = 0;
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 720f * dt);
        }

        void Animate(float dt)
        {
            Vector3 v = agent.velocity; v.y = 0;
            if (v.sqrMagnitude > 0.05f && state != S.Stunned)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(v.normalized), 540f * dt);
            if (anim && anim.runtimeAnimatorController)
            {
                // smooth the agent's (noisy) speed first, so idle/walk/run never flickers
                smoothSpeed = Mathf.Lerp(smoothSpeed, v.magnitude, 1f - Mathf.Exp(-6f * dt));
                float k = smoothSpeed < 0.25f ? 0f : (state == S.Chase ? Mathf.Lerp(0.6f, 1f, smoothSpeed / animMax) : 0.5f);
                anim.SetFloat(AnimParams.Speed, k, 0.2f, dt);
                // match the walk/run cycle to the ground speed (eased, so it never jumps)
                float target = state == S.Stunned ? 1f : GameAssets.I.Enemy(Type).PlaybackSpeed(k, smoothSpeed);
                animSpeed = Mathf.Lerp(animSpeed, target, 1f - Mathf.Exp(-5f * dt));
                anim.speed = animSpeed;
            }
        }

        void OnDestroy()
        {
            if (marker != null && marker.rt) Destroy(marker.rt.gameObject);
        }
    }
}
