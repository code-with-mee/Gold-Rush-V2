using System.Collections.Generic;
using UnityEngine;

namespace GoldRush
{
    /// <summary>Global audio player for synthesized sound effects and background music.</summary>
    public class AudioFX : MonoBehaviour
    {
        const int PoolSize = 8;
        const float MusicVolume = 0.3f;
        const float TensionPitch = 1.07f;
        const float TensionVolume = 1.15f;

        static AudioFX inst;
        static bool quitting;

        Dictionary<string, AudioClip> clips;
        AudioSource[] pool;
        int next;
        AudioSource music;

        bool tension;
        float tensionVolCur = 1f;
        float duckAmount;
        float duckTimer;
        float duckCur;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            inst = null;
            quitting = false;
        }

        static AudioFX I
        {
            get
            {
                if (inst != null) return inst;
                if (quitting || !Application.isPlaying) return null;
                var go = new GameObject("[AudioFX]");
                DontDestroyOnLoad(go);
                go.AddComponent<AudioFX>(); // Awake assigns inst
                return inst;
            }
        }

        void Awake()
        {
            if (inst != null && inst != this)
            {
                Destroy(gameObject);
                return;
            }
            inst = this;
            Init();
        }

        void Init()
        {
            if (clips != null) return;
            clips = Synth.BuildAll();

            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = false;
                s.spatialBlend = 0f;
                s.priority = 64;
                pool[i] = s;
            }

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.priority = 0;
            music.volume = MusicVolume;
            music.mute = !Save.Data.music;
        }

        void OnApplicationQuit() { quitting = true; }

        void OnDestroy()
        {
            if (inst == this) inst = null;
        }

        // ------------------------------------------------------------------ public API

        public static void Play(string id, float volume = 1f, float pitch = 1f)
        {
            if (!Save.Data.sfx || string.IsNullOrEmpty(id)) return;
            var i = I;
            if (i == null) return;
            i.PlayInternal(id, volume, pitch);
        }

        public static void StartMusic()
        {
            var i = I;
            if (i == null) return;
            i.music.mute = !Save.Data.music;
            if (i.music.isPlaying) return;
            AudioClip clip;
            if (!i.clips.TryGetValue("music", out clip) || clip == null) return;
            i.music.clip = clip;
            i.music.loop = true;
            i.music.pitch = i.tension ? TensionPitch : 1f;
            i.music.volume = i.ComputeMusicVolume();
            i.music.Play();
        }

        public static void RefreshMute()
        {
            var i = I;
            if (i == null) return;
            i.music.mute = !Save.Data.music;
            if (!Save.Data.sfx)
            {
                for (int k = 0; k < i.pool.Length; k++)
                    if (i.pool[k] != null && i.pool[k].isPlaying) i.pool[k].Stop();
            }
        }

        public static void SetTension(bool chased)
        {
            var i = I;
            if (i == null) return;
            i.tension = chased;
        }

        public static void Duck(float amount, float seconds)
        {
            var i = I;
            if (i == null) return;
            amount = Mathf.Clamp01(amount);
            if (i.duckTimer > 0f) i.duckAmount = Mathf.Max(i.duckAmount, amount);
            else i.duckAmount = amount;
            i.duckTimer = Mathf.Max(i.duckTimer, seconds);
        }

        // ------------------------------------------------------------------ internals

        void PlayInternal(string id, float volume, float pitch)
        {
            AudioClip clip;
            if (clips == null || !clips.TryGetValue(id, out clip) || clip == null)
            {
                Debug.LogWarning("[AudioFX] Unknown sound id: " + id);
                return;
            }

            AudioSource src = null;
            for (int k = 0; k < PoolSize; k++)
            {
                var s = pool[(next + k) % PoolSize];
                if (!s.isPlaying) { src = s; next = (next + k + 1) % PoolSize; break; }
            }
            if (src == null)
            {
                src = pool[next];
                next = (next + 1) % PoolSize;
            }

            src.Stop();
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            src.Play();
        }

        float ComputeMusicVolume()
        {
            return MusicVolume * tensionVolCur * (1f - duckCur);
        }

        void Update()
        {
            if (music == null) return;
            float dt = Time.unscaledDeltaTime;

            float targetPitch = tension ? TensionPitch : 1f;
            float targetVol = tension ? TensionVolume : 1f;
            float k = 1f - Mathf.Exp(-dt * 1.5f);
            music.pitch = Mathf.Lerp(music.pitch, targetPitch, k);
            if (Mathf.Abs(music.pitch - targetPitch) < 0.0005f) music.pitch = targetPitch;
            tensionVolCur = Mathf.Lerp(tensionVolCur, targetVol, k);

            if (duckTimer > 0f) duckTimer -= dt;
            float targetDuck = duckTimer > 0f ? duckAmount : 0f;
            float rate = targetDuck > duckCur ? 10f : 2f;
            duckCur = Mathf.Lerp(duckCur, targetDuck, 1f - Mathf.Exp(-dt * rate));
            if (targetDuck == 0f && duckCur < 0.001f) { duckCur = 0f; duckAmount = 0f; }

            music.volume = ComputeMusicVolume();
        }
    }
}
