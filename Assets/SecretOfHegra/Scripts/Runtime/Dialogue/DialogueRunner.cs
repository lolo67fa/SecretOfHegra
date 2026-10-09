using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Plays dialogue sequences: subtitle + optional voice clip per line, timed by clip length or reading
    /// speed. Requests are queued. The player may advance a line early. Waits while the game is paused.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] SubtitleDisplay m_Subtitles;
        [SerializeField] AudioSource m_VoiceSource;
        [SerializeField] VoiceoverBank m_Voiceover;

        public DialogueDatabase Database { get; set; }
        public Func<bool> IsPaused;

        /// <summary>Raised with the speaker id at the start of each line (NPCs use it to gesture).</summary>
        public event Action<string> LineStarted;
        /// <summary>Raised with the line key at the start of each line (the theater performance cues on it).</summary>
        public event Action<string> LineKeyStarted;
        public event Action SequenceFinished;

        struct Request { public string id; public Action onComplete; }

        readonly Queue<Request> m_Queue = new Queue<Request>();
        Coroutine m_Routine;
        bool m_AdvanceRequested;
        bool m_SkipSequenceRequested;

        public bool IsPlaying => m_Routine != null;
        public string CurrentSequence { get; private set; }
        public string CurrentSpeaker { get; private set; }

        public SubtitleDisplay Subtitles { get => m_Subtitles; set => m_Subtitles = value; }
        /// <summary>Speech bubble beside the speaking character; the subtitle bar is only the fallback.</summary>
        public SpeechBubble Bubble { get; set; }
        /// <summary>Speakers farther than this from the player use the subtitle bar instead of a bubble.</summary>
        public float bubbleRange = 18f;
        public AudioSource VoiceSource { get => m_VoiceSource; set => m_VoiceSource = value; }
        public VoiceoverBank Voiceover { get => m_Voiceover; set => m_Voiceover = value; }

        public void Play(string sequenceId, Action onComplete = null)
        {
            if (Database == null || !Database.Has(sequenceId))
            {
                Debug.LogWarning($"[Hegra] Missing dialogue sequence '{sequenceId}'.");
                onComplete?.Invoke();
                return;
            }
            m_Queue.Enqueue(new Request { id = sequenceId, onComplete = onComplete });
            if (m_Routine == null)
                m_Routine = StartCoroutine(Run());
        }

        /// <summary>Interrupts everything without invoking completion callbacks (used on reload/relocation).</summary>
        public void StopAll()
        {
            m_Queue.Clear();
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = null;
            CurrentSequence = null;
            if (m_VoiceSource) m_VoiceSource.Stop();
            if (m_Subtitles) m_Subtitles.Hide();
            if (Bubble) Bubble.EndDialogue();
        }

        /// <summary>Trigger: the first press finishes the typing line, the next one continues.</summary>
        public void Advance()
        {
            if (Bubble && Bubble.IsTyping) { Bubble.CompleteTyping(); return; }
            m_AdvanceRequested = true;
        }

        /// <summary>Finishes the current sequence immediately (its completion callback still runs).</summary>
        public void SkipSequence() => m_SkipSequenceRequested = true;

        IEnumerator Run()
        {
            while (m_Queue.Count > 0)
            {
                var req = m_Queue.Dequeue();
                CurrentSequence = req.id;
                m_SkipSequenceRequested = false;
                var seq = Database.Get(req.id);
                foreach (var line in seq.lines)
                {
                    if (m_SkipSequenceRequested) break;
                    yield return PlayLine(line);
                }
                if (m_VoiceSource) m_VoiceSource.Stop();
                if (m_Subtitles) m_Subtitles.Hide();
                CurrentSequence = null;
                if (Bubble) Bubble.EndDialogue();
                CurrentSpeaker = null;
                SequenceFinished?.Invoke();
                try { req.onComplete?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
                yield return null;
            }
            m_Routine = null;
        }

        IEnumerator PlayLine(DialogueDatabase.Line line)
        {
            m_AdvanceRequested = false;
            CurrentSpeaker = line.speaker;
            var speaker = Database.GetSpeaker(line.speaker);
            string speakerName = speaker != null ? Loc.Get(speaker.nameKey) : "";
            string text = Loc.Get(line.key);
            Color nameColor = Color.white;
            if (speaker != null && !string.IsNullOrEmpty(speaker.color))
                ColorUtility.TryParseHtmlString(speaker.color, out nameColor);

            var npc = Bubble ? NpcFigure.FindSpeaker(line.speaker) : null;
            bool inBubble = npc && InBubbleRange(npc);
            if (inBubble)
            {
                if (m_Subtitles) m_Subtitles.Hide();
                Bubble.ShowLine(npc, speakerName, text, nameColor);
            }
            else
            {
                if (Bubble && Bubble.IsVisible) Bubble.Close();
                if (m_Subtitles) m_Subtitles.Show(speakerName, text, nameColor);
            }

            AudioClip clip = m_Voiceover ? m_Voiceover.Get(line.key, Loc.Current) : null;
            float duration = clip ? clip.length + 0.35f : DialogueDatabase.EstimateDuration(text, line.duration);
            if (inBubble) duration = Mathf.Max(duration, Bubble.TypingSeconds + 1.8f);
            if (clip && m_VoiceSource)
            {
                m_VoiceSource.clip = clip;
                m_VoiceSource.Play();
            }

            LineStarted?.Invoke(line.speaker);
            LineKeyStarted?.Invoke(line.key);

            float t = 0f;
            const float minShow = 0.6f;
            while (t < duration)
            {
                if (m_SkipSequenceRequested) yield break;
                if (m_AdvanceRequested && t > minShow) break;
                m_AdvanceRequested = false;
                bool paused = IsPaused != null && IsPaused();
                if (m_VoiceSource && clip)
                {
                    if (paused && m_VoiceSource.isPlaying) m_VoiceSource.Pause();
                    else if (!paused && !m_VoiceSource.isPlaying && t < clip.length) m_VoiceSource.UnPause();
                }
                if (!paused) t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (m_VoiceSource && m_VoiceSource.isPlaying) m_VoiceSource.Stop();
        }

        bool InBubbleRange(NpcFigure npc)
        {
            var gm = GameManager.Instance;
            if (!gm || !gm.Rig) return false;
            var d = npc.transform.position - gm.Rig.Head.position; d.y = 0f;
            return d.magnitude <= bubbleRange;
        }
    }
}
