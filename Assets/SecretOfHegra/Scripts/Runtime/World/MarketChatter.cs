using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Market townspeople who speak when the player comes close: the person turns, gestures and says one short
    /// line shown as a bubble above the head (Arabic/English) with an unintelligible "babble" voice at their own
    /// pitch. A real recording named after the line key and language replaces the babble when present.
    /// Only one person talks at a time, and nobody talks over story dialogue or the kohl purchase.
    /// </summary>
    public class MarketChatter : MonoBehaviour
    {
        [Serializable]
        public class Speaker
        {
            public NpcFigure npc;
            public string[] lineKeys;
            public float pitch = 1f;
            public TextMeshPro bubble;
            public Transform bubbleRoot;
            public Renderer bubbleBack;
            public AudioSource voice;
            [NonSerialized] public int nextLine;
            [NonSerialized] public float readyAt;
        }

        [Serializable]
        public class RecordedLine
        {
            public string key;
            public Language language;
            public AudioClip clip;
        }

        public List<Speaker> speakers = new List<Speaker>();
        public AudioClip[] syllables;
        public List<RecordedLine> recorded = new List<RecordedLine>();
        public float triggerDistance = 2.6f;
        public float globalGap = 3.5f;
        public float speakerCooldown = 25f;

        static readonly QuestStep[] s_Quiet = { QuestStep.MarketLegend, QuestStep.MarketBuyKohl, QuestStep.MarketApplyKohl };

        Speaker m_Active;
        float m_NextAllowed;

        /// <summary>True while a townsperson is speaking (story dialogue and hints can check this).</summary>
        public bool Speaking => m_Active != null;

        void Start()
        {
            foreach (var s in speakers) if (s.bubbleRoot) s.bubbleRoot.gameObject.SetActive(false);
        }

        void Update()
        {
            var g = GameManager.Instance;
            if (!g || g.State == null) return;
            bool blocked = !g.InGame || g.MenuOpen || g.Dialogue.IsPlaying || Array.IndexOf(s_Quiet, g.State.step) >= 0;
            if (m_Active != null)
            {
                if (blocked) StopSpeaking();
                else Billboard(m_Active, g.Rig.Head.position);
                return;
            }
            if (blocked || Time.time < m_NextAllowed) return;

            var head = g.Rig.Head.position;
            Speaker best = null;
            float bestD = triggerDistance;
            foreach (var s in speakers)
            {
                if (!s.npc || Time.time < s.readyAt) continue;
                var d = s.npc.transform.position - head; d.y = 0;
                if (d.magnitude < bestD) { bestD = d.magnitude; best = s; }
            }
            if (best != null) StartCoroutine(Speak(best, g));
        }

        string PickLine(Speaker s, GameManager g)
        {
            // the objective hint takes priority while it is relevant
            var hint = "chatter.hint." + g.State.step;
            if (Loc.Has(hint) && UnityEngine.Random.value < 0.6f) return hint;
            if (s.lineKeys == null || s.lineKeys.Length == 0) return null;
            var key = s.lineKeys[s.nextLine % s.lineKeys.Length];
            s.nextLine++;
            return key;
        }

        IEnumerator Speak(Speaker s, GameManager g)
        {
            var key = PickLine(s, g);
            if (key == null) { s.readyAt = Time.time + speakerCooldown; yield break; }
            m_Active = s;
            var text = Loc.Get(key);
            float duration = Mathf.Clamp(1.2f + text.Length * 0.06f, 2.4f, 6f);

            bool faced = s.npc.facePlayer;
            s.npc.facePlayer = true;
            s.npc.Play(NpcGesture.Talk, duration);
            if (s.bubble) Loc.Apply(s.bubble, text, TextAlignmentOptions.Center);
            if (s.bubbleRoot) s.bubbleRoot.gameObject.SetActive(true);
            FitBack(s);
            Billboard(s, g.Rig.Head.position);

            var clip = FindRecording(key, Loc.Current);
            if (clip && s.voice) { s.voice.pitch = 1f; s.voice.PlayOneShot(clip); duration = Mathf.Max(duration, clip.length + 0.4f); }
            else if (s.voice && syllables != null && syllables.Length > 0) StartCoroutine(Babble(s, duration * 0.85f));

            float t = 0f;
            while (t < duration && m_Active == s) { t += Time.deltaTime; yield return null; }
            s.npc.facePlayer = faced;
            if (m_Active == s) StopSpeaking();
        }

        IEnumerator Babble(Speaker s, float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end && m_Active == s)
            {
                var syl = syllables[UnityEngine.Random.Range(0, syllables.Length)];
                s.voice.pitch = s.pitch * UnityEngine.Random.Range(0.92f, 1.12f);
                s.voice.PlayOneShot(syl, 0.8f);
                yield return new WaitForSeconds(syl.length / s.voice.pitch * 0.85f + UnityEngine.Random.Range(0.01f, 0.06f));
                if (UnityEngine.Random.value < 0.12f) yield return new WaitForSeconds(0.18f); // breath between "words"
            }
        }

        AudioClip FindRecording(string key, Language lang)
        {
            foreach (var r in recorded)
                if (r != null && r.clip && r.key == key && r.language == lang) return r.clip;
            return null;
        }

        void StopSpeaking()
        {
            if (m_Active == null) return;
            var s = m_Active;
            if (s.bubbleRoot) s.bubbleRoot.gameObject.SetActive(false);
            if (s.voice) s.voice.Stop();
            s.readyAt = Time.time + speakerCooldown;
            m_Active = null;
            m_NextAllowed = Time.time + globalGap;
        }

        static void Billboard(Speaker s, Vector3 head)
        {
            if (!s.bubbleRoot) return;
            var d = s.bubbleRoot.position - head; d.y = 0;
            if (d.sqrMagnitude > 0.0001f) s.bubbleRoot.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        static void FitBack(Speaker s)
        {
            if (!s.bubble || !s.bubbleBack) return;
            s.bubble.ForceMeshUpdate();
            var b = s.bubble.textBounds.size;
            s.bubbleBack.transform.localScale = new Vector3(Mathf.Max(0.3f, b.x + 0.12f), Mathf.Max(0.12f, b.y + 0.08f), 1f);
        }
    }
}
