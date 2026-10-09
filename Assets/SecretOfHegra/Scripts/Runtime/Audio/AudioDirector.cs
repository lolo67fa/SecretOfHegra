using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SecretOfHegra
{
    public enum Sfx
    {
        UiClick, SignChime, Coin, Carve, StoneDoor, Footstep, Camel, Grab, KohlApply, WaterGate, Chest, Fail,
        // appended (serialized by index): story stingers and UI
        LegendSting, RevealSting, UiOpen, UiConfirm
    }

    /// <summary>Static entry point so any component can play a sound without scene lookups.</summary>
    public static class HegraAudio
    {
        public static AudioDirector Director;

        public static void PlayUiClick() { if (Director) Director.Play2D(Sfx.UiClick, 0.6f); }
        public static void Play2D(Sfx s, float volume = 1f) { if (Director) Director.Play2D(s, volume); }
        public static void PlayAt(Sfx s, Vector3 pos, float volume = 1f) { if (Director) Director.PlayAt(s, pos, volume); }
        /// <summary>A key story moment: the dramatic hit plus a short swell of the music.</summary>
        public static void Reveal() { if (Director) { Director.Play2D(Sfx.RevealSting, 0.9f); Director.Swell(6f); } }
    }

    /// <summary>
    /// Mixer volumes, the journey music, zone-based ambience crossfades, global desert wind and pooled one-shot SFX.
    /// With a journey track assigned, that single 2D source plays from the main menu to the ending screen (zones
    /// and overrides no longer change the music). It ducks under narration, pauses with the app/headset,
    /// and resumes at the same point after the scene reloads (New Game / Continue / Return to menu).
    /// The zone whose center is nearest the listener (within its radius) owns the ambience bed.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        [System.Serializable]
        public class SfxClip { public Sfx id; public AudioClip clip; }

        [SerializeField] AudioMixer m_Mixer;
        [SerializeField] AudioMixerGroup m_MusicGroup;
        [SerializeField] AudioMixerGroup m_AmbienceGroup;
        [SerializeField] AudioMixerGroup m_SfxGroup;
        [SerializeField] AudioMixerGroup m_DialogueGroup;
        [SerializeField] AudioClip m_WindLoop;
        [SerializeField] List<SfxClip> m_Sfx = new List<SfxClip>();
        [SerializeField] Transform m_Listener;
        [SerializeField] float m_CrossfadeSeconds = 3f;

        [Header("Journey music")]
        [SerializeField] AudioClip m_JourneyMusic;
        [SerializeField, Range(0f, 1f)] float m_JourneyVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] float m_DuckFactor = 0.45f;
        [SerializeField] float m_DuckSeconds = 0.6f;
        [Tooltip("Darker variation that takes over while a legend is being told.")]
        [SerializeField] AudioClip m_LegendMusic;
        [SerializeField] float m_SwellBoost = 1.6f;

        public AudioMixerGroup DialogueGroup => m_DialogueGroup;
        public AudioMixerGroup SfxGroup => m_SfxGroup;
        public AudioMixerGroup AmbienceGroup => m_AmbienceGroup;
        public AudioMixerGroup MusicGroup => m_MusicGroup;
        public Transform Listener { get => m_Listener; set => m_Listener = value; }
        public AudioClip JourneyMusic => m_JourneyMusic;
        public AudioSource JourneySource => m_Journey;

        /// <summary>Playback position carried over a scene reload so the track continues instead of restarting.</summary>
        static float s_JourneyTime = -1f;
        AudioSource m_Journey;
        AudioSource m_Legend;
        float m_LegendMix;       // 0 = journey track, 1 = legend track
        float m_SwellUntil;
        string m_LastSequence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_JourneyTime = -1f; // play mode may run without a domain reload

        readonly Dictionary<Sfx, AudioClip> m_SfxLookup = new Dictionary<Sfx, AudioClip>();
        readonly List<AudioSource> m_Pool = new List<AudioSource>();
        int m_PoolIndex;

        AudioSource[] m_Music = new AudioSource[2];
        AudioSource[] m_Ambience = new AudioSource[2];
        int m_MusicActive, m_AmbienceActive;
        AudioSource m_Wind;
        AmbienceZone m_CurrentZone;
        AudioClip m_OverrideMusic;
        float m_TimeOfNightIntensity;

        void Awake()
        {
            HegraAudio.Director = this;
            foreach (var s in m_Sfx)
                if (s.clip) m_SfxLookup[s.id] = s.clip;
            for (int i = 0; i < 2; i++)
            {
                if (!m_JourneyMusic) m_Music[i] = MakeLoop("Music" + i, m_MusicGroup); // zone music only without a journey track
                m_Ambience[i] = MakeLoop("Ambience" + i, m_AmbienceGroup);
            }
            if (m_JourneyMusic)
            {
                m_Journey = MakeLoop("JourneyMusic", m_MusicGroup);
                m_Journey.clip = m_JourneyMusic;
                m_Journey.priority = 0;
                m_Journey.ignoreListenerPause = false;
                if (s_JourneyTime > 0f) m_Journey.time = s_JourneyTime % m_JourneyMusic.length;
                m_Journey.Play();
                if (m_LegendMusic)
                {
                    m_Legend = MakeLoop("LegendMusic", m_MusicGroup);
                    m_Legend.clip = m_LegendMusic;
                    m_Legend.priority = 0;
                }
            }
            m_Wind = MakeLoop("Wind", m_AmbienceGroup);
            if (m_WindLoop)
            {
                m_Wind.clip = m_WindLoop;
                m_Wind.volume = 0.35f;
                m_Wind.Play();
            }
            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject("Sfx" + i);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.outputAudioMixerGroup = m_SfxGroup;
                src.dopplerLevel = 0f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 1f;
                src.maxDistance = 25f;
                m_Pool.Add(src);
            }
        }

        void OnDestroy()
        {
            if (HegraAudio.Director == this) HegraAudio.Director = null;
        }

        void UpdateJourney()
        {
            if (!m_Journey) return;
            var gm = GameManager.Instance;
            bool hold = gm && (!gm.AppFocused || !gm.HeadsetPresent);
            if (hold && m_Journey.isPlaying) m_Journey.Pause();
            else if (!hold && !m_Journey.isPlaying) m_Journey.UnPause();
            if (!m_Journey.isPlaying && !hold) m_Journey.Play();
            bool narrating = gm && gm.Dialogue && gm.Dialogue.IsPlaying;

            // a legend starts: mysterious stinger, and the darker variation fades in under the storyteller
            var seq = gm && gm.Dialogue ? gm.Dialogue.CurrentSequence : null;
            bool legend = !string.IsNullOrEmpty(seq) && seq.Contains("legend");
            if (seq != m_LastSequence)
            {
                if (legend) { Play2D(Sfx.LegendSting, 0.8f); Swell(4f); }
                m_LastSequence = seq;
            }
            m_LegendMix = Mathf.MoveTowards(m_LegendMix, legend && m_Legend ? 1f : 0f, Time.unscaledDeltaTime / 2.5f);

            float swell = Time.unscaledTime < m_SwellUntil ? m_SwellBoost : 1f;
            float target = m_JourneyVolume * (narrating ? m_DuckFactor : 1f) * swell;
            float rate = Time.unscaledDeltaTime / Mathf.Max(0.05f, m_DuckSeconds) * m_JourneyVolume;
            m_Journey.volume = Mathf.MoveTowards(m_Journey.volume, target * (1f - m_LegendMix), rate);
            if (m_Legend)
            {
                float lt = target * 1.1f * m_LegendMix;
                if (lt > 0.001f && !m_Legend.isPlaying && !hold) m_Legend.Play();
                if (hold && m_Legend.isPlaying) m_Legend.Pause();
                m_Legend.volume = Mathf.MoveTowards(m_Legend.volume, lt, rate);
                if (m_LegendMix <= 0f && m_Legend.volume <= 0.001f && m_Legend.isPlaying) m_Legend.Stop();
            }
            // remembered every frame: during a scene unload the source already reports time 0
            if (m_Journey.isPlaying) s_JourneyTime = m_Journey.time;
        }

        /// <summary>Lifts the music for a few seconds at an important story moment.</summary>
        public void Swell(float seconds) => m_SwellUntil = Mathf.Max(m_SwellUntil, Time.unscaledTime + seconds);

        AudioSource MakeLoop(string name, AudioMixerGroup group)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 0f;
            src.outputAudioMixerGroup = group;
            return src;
        }

        public void ApplySettings(SettingsData s)
        {
            if (!m_Mixer) return;
            m_Mixer.SetFloat("MusicVolume", ToDb(s.musicVolume));
            m_Mixer.SetFloat("AmbienceVolume", ToDb(s.ambienceVolume));
            m_Mixer.SetFloat("SfxVolume", ToDb(s.sfxVolume));
            m_Mixer.SetFloat("DialogueVolume", ToDb(s.dialogueVolume));
        }

        static float ToDb(float v) => v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f;

        /// <summary>0 at the start of the night, 1 when the moon reaches the mountains. Wind rises with it.</summary>
        public void SetNightProgress(float t)
        {
            m_TimeOfNightIntensity = Mathf.Clamp01(t);
        }

        /// <summary>Forces a music bed regardless of zone (e.g. the theater performance or the finale choir).</summary>
        public void SetMusicOverride(AudioClip clip)
        {
            if (m_Journey) return; // one track for the whole journey
            m_OverrideMusic = clip;
            if (clip) CrossfadeTo(m_Music, ref m_MusicActive, clip, 0.8f);
            else m_CurrentZone = null; // force the zone to be re-evaluated
        }

        public void Play2D(Sfx id, float volume = 1f)
        {
            var src = Next();
            if (!src || !m_SfxLookup.TryGetValue(id, out var clip)) return;
            src.transform.localPosition = Vector3.zero;
            src.spatialBlend = 0f;
            src.pitch = 1f;
            src.PlayOneShot(clip, volume);
        }

        public void PlayAt(Sfx id, Vector3 position, float volume = 1f)
        {
            var src = Next();
            if (!src || !m_SfxLookup.TryGetValue(id, out var clip)) return;
            src.transform.position = position;
            src.spatialBlend = 1f;
            // footsteps vary more so a walk never sounds like one sample repeated
            src.pitch = id == Sfx.Footstep ? Random.Range(0.82f, 1.18f) : Random.Range(0.94f, 1.06f);
            src.PlayOneShot(clip, volume);
        }

        AudioSource Next()
        {
            if (m_Pool.Count == 0) return null;
            m_PoolIndex = (m_PoolIndex + 1) % m_Pool.Count;
            return m_Pool[m_PoolIndex];
        }

        void Update()
        {
            if (m_Wind && m_Wind.clip)
                m_Wind.volume = Mathf.Lerp(0.22f, 0.5f, m_TimeOfNightIntensity);

            if (m_Listener)
            {
                var zone = AmbienceZone.Find(m_Listener.position);
                if (zone != m_CurrentZone)
                {
                    m_CurrentZone = zone;
                    if (!m_OverrideMusic && !m_Journey)
                        CrossfadeTo(m_Music, ref m_MusicActive, zone ? zone.music : null, zone ? zone.musicVolume : 0f);
                    CrossfadeTo(m_Ambience, ref m_AmbienceActive, zone ? zone.ambience : null, zone ? zone.ambienceVolume : 0f);
                }
            }

            UpdateJourney();
            if (m_Music[0]) StepFade(m_Music, m_MusicActive);
            StepFade(m_Ambience, m_AmbienceActive);
        }

        readonly Dictionary<AudioSource, float> m_Targets = new Dictionary<AudioSource, float>();

        void CrossfadeTo(AudioSource[] pair, ref int active, AudioClip clip, float volume)
        {
            var current = pair[active];
            if (current.clip == clip && current.isPlaying)
            {
                m_Targets[current] = volume;
                return;
            }
            int next = 1 - active;
            m_Targets[pair[active]] = 0f;
            var src = pair[next];
            src.clip = clip;
            if (clip)
            {
                src.volume = 0f;
                src.Play();
            }
            m_Targets[src] = clip ? volume : 0f;
            active = next;
        }

        void StepFade(AudioSource[] pair, int active)
        {
            float step = Time.unscaledDeltaTime / Mathf.Max(0.1f, m_CrossfadeSeconds);
            foreach (var src in pair)
            {
                m_Targets.TryGetValue(src, out var target);
                src.volume = Mathf.MoveTowards(src.volume, target, step);
                if (src.volume <= 0.001f && target <= 0f && src.isPlaying) src.Stop();
            }
        }
    }
}
