using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra
{
    /// <summary>
    /// The inscription on the Elephant Rock (an AlUla landmark brought into this imagined journey). The lamp
    /// uncovers a row of stylized, Nabataean-inspired glyphs; three of them then glow in order and the player
    /// points at the same three (ray + trigger, from any distance, seated or standing). A wrong pick just
    /// replays the sequence. Completing it raises <see cref="Solved"/>; the stop grants Sign 1.
    /// </summary>
    public class ElephantRockPuzzle : MonoBehaviour
    {
        public LampRevealTarget reveal;
        public Transform face;
        public List<TMP_Text> glyphs = new List<TMP_Text>();
        public List<Renderer> glyphGlows = new List<Renderer>();
        public List<XRSimpleInteractable> glyphButtons = new List<XRSimpleInteractable>();
        public int[] order = { 0, 1, 2 };

        [Header("World response")]
        public Renderer faceGlow;
        public Renderer gapGlow;

        [Header("Timing")]
        public float showSeconds = 0.85f;
        public float gapSeconds = 0.3f;

        public Color carvedColor = new Color(0.52f, 0.3f, 0.22f);
        public Color goldColor = new Color(1f, 0.82f, 0.45f);

        public event Action Solved;
        public event Action Mistake;

        public bool Revealed { get; private set; }
        public bool IsSolved => m_Sequence != null && m_Sequence.Solved;
        public bool Showing { get; private set; }
        public int Mistakes => m_Sequence?.Mistakes ?? 0;
        public int Expected => m_Sequence?.Expected ?? -1;

        /// <summary>The stop turns this on for hint level 3 or after repeated mistakes: the next glyph pulses softly.</summary>
        [NonSerialized] public bool showNextHint;
        [NonSerialized] public bool interactive = true;

        GlyphSequence m_Sequence;
        readonly float[] m_Lit = new float[8];
        int m_HoverIndex = -1;
        float m_SolvedGlow;
        MaterialPropertyBlock m_Mpb;
        static readonly int k_Intensity = Shader.PropertyToID("_Intensity");

        void Awake()
        {
            m_Sequence = new GlyphSequence(order);
            for (int i = 0; i < glyphButtons.Count; i++)
            {
                int index = i;
                var b = glyphButtons[i];
                if (!b) continue;
                b.selectEntered.AddListener(_ => Pick(index));
                b.hoverEntered.AddListener(_ => m_HoverIndex = index);
                b.hoverExited.AddListener(_ => { if (m_HoverIndex == index) m_HoverIndex = -1; });
            }
            if (reveal) reveal.Revealed += () => Revealed = true;
            ApplyVisuals(true);
        }

        /// <summary>Brings the rock in line with a loaded save.</summary>
        public void Restore(bool revealed, bool solved)
        {
            StopAllCoroutines();
            Showing = false;
            m_Sequence = new GlyphSequence(order);
            Revealed = revealed || solved;
            if (reveal) reveal.SetRevealed(Revealed);
            if (solved) m_Sequence.MarkSolved();
            m_SolvedGlow = solved ? 1f : 0f;
            for (int i = 0; i < m_Lit.Length; i++) m_Lit[i] = 0f;
            ApplyVisuals(true);
        }

        /// <summary>Plays the three-glyph sequence (also the "replay" button). Input waits until it ends.</summary>
        public void PlaySequence(float delay = 0f)
        {
            if (!Revealed || IsSolved) return;
            StopAllCoroutines();
            StartCoroutine(SequenceRoutine(delay));
        }

        IEnumerator SequenceRoutine(float delay)
        {
            Showing = true;
            m_Sequence.Restart();
            for (int i = 0; i < m_Lit.Length; i++) m_Lit[i] = 0f;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (int s = 0; s < m_Sequence.Length; s++)
            {
                int g = m_Sequence[s];
                if (g < glyphs.Count && glyphs[g]) HegraAudio.PlayAt(Sfx.UiClick, glyphs[g].transform.position, 0.5f);
                for (float t = 0; t < showSeconds; t += Time.deltaTime)
                {
                    m_Lit[g] = Mathf.Sin(Mathf.Clamp01(t / showSeconds) * Mathf.PI);
                    yield return null;
                }
                m_Lit[g] = 0f;
                yield return new WaitForSeconds(gapSeconds);
            }
            Showing = false;
        }

        /// <summary>Called by the glyph's interactable (and by automation).</summary>
        public void Pick(int glyph)
        {
            if (!interactive || !Revealed || IsSolved || Showing) return;
            var pos = glyph < glyphs.Count && glyphs[glyph] ? glyphs[glyph].transform.position : transform.position;
            switch (m_Sequence.Select(glyph))
            {
                case GlyphSequence.Result.Progress:
                    m_Lit[glyph] = 1f;
                    HegraAudio.PlayAt(Sfx.UiClick, pos, 0.8f);
                    break;
                case GlyphSequence.Result.Wrong:
                    for (int i = 0; i < m_Lit.Length; i++) m_Lit[i] = 0f;
                    HegraAudio.PlayAt(Sfx.Fail, pos, 0.4f);
                    Mistake?.Invoke();
                    PlaySequence(1.1f);
                    break;
                case GlyphSequence.Result.Solved:
                    m_Lit[glyph] = 1f;
                    StartCoroutine(SolveRoutine());
                    break;
            }
        }

        IEnumerator SolveRoutine()
        {
            HegraAudio.PlayAt(Sfx.Carve, face ? face.position : transform.position, 0.9f);
            Solved?.Invoke();
            for (float t = 0; t < 2.5f; t += Time.deltaTime)
            {
                m_SolvedGlow = Mathf.SmoothStep(0f, 1f, t / 2.5f);
                yield return null;
            }
            m_SolvedGlow = 1f;
        }

        void Update() => ApplyVisuals(false);

        void ApplyVisuals(bool force)
        {
            bool solved = IsSolved;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            for (int i = 0; i < glyphs.Count; i++)
            {
                float lit = solved ? 1f : m_Lit[i];
                if (!solved && Revealed && !Showing)
                {
                    if (i == m_HoverIndex && interactive) lit = Mathf.Max(lit, 0.35f);
                    if (showNextHint && i == m_Sequence.Expected) lit = Mathf.Max(lit, 0.25f + 0.35f * pulse);
                }
                if (glyphs[i] && (Revealed || solved))
                {
                    var c = Color.Lerp(carvedColor, goldColor, lit);
                    c.a = 1f;
                    glyphs[i].color = c;
                }
                if (i < glyphGlows.Count) SetGlow(glyphGlows[i], lit * 0.55f);
            }
            SetGlow(faceGlow, m_SolvedGlow * 0.35f);
            SetGlow(gapGlow, m_SolvedGlow * 0.5f);
        }

        void SetGlow(Renderer r, float intensity)
        {
            if (!r) return;
            m_Mpb ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(m_Mpb);
            m_Mpb.SetFloat(k_Intensity, intensity);
            r.SetPropertyBlock(m_Mpb);
            r.enabled = intensity > 0.01f;
        }
    }
}
