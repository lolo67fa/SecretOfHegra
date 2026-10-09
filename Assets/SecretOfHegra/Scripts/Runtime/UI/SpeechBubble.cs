using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SecretOfHegra
{
    /// <summary>
    /// World-space speech bubble that sits beside the head of whoever is talking (on the player's side, a little to
    /// the right of the head), with a tail pointing at the speaker. It shows the speaker's name in their colour,
    /// types the line out with a soft blip, grows to fit the text (no clipped Arabic lines), turns to face the player
    /// (yaw only) and keeps a readable size at any distance. Choice buttons for the current objective appear under
    /// the text when the objective belongs to a nearby character.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        const float Width = 640f, Pad = 24f, NameH = 42f, ChoiceH = 66f, Gap = 10f;
        const float CharsPerSecond = 40f;

        Canvas m_Canvas;
        RectTransform m_Rect, m_Tail, m_Choices;
        CanvasGroup m_Group;
        Image m_Bg;
        TextMeshProUGUI m_Name, m_Line;
        AudioSource m_Blip;
        readonly List<GameObject> m_Buttons = new List<GameObject>();

        NpcFigure m_Npc;
        string m_RawName, m_RawLine;
        Color m_NameColor = Color.white;
        bool m_Typing, m_Visible, m_ChoiceOnly;
        float m_Shown, m_Appear;
        int m_LastBlip;
        float m_Scale = 1f;
        float m_NextChoiceCheck;
        Material m_OnTopUi;
        (string key, Action action)[] m_Actions;

        public bool IsTyping => m_Typing;
        public bool IsVisible => m_Visible;
        /// <summary>Seconds the current line needs to finish typing.</summary>
        public float TypingSeconds => m_Line ? m_Line.textInfo.characterCount / CharsPerSecond : 0f;

        public static SpeechBubble Create(Camera cam)
        {
            var root = new GameObject("SpeechBubble");
            var b = root.AddComponent<SpeechBubble>();
            b.Build(cam);
            return b;
        }

        void Build(Camera cam)
        {
            m_Canvas = UIFactory.CreateCanvas("BubbleCanvas", transform, new Vector2(Width, 200), cam, 20);
            m_Rect = (RectTransform)m_Canvas.transform;
            m_Group = m_Canvas.gameObject.AddComponent<CanvasGroup>();
            m_Group.alpha = 0f;
            m_Group.blocksRaycasts = false;

            // tail: a rotated square half-hidden behind the left edge, pointing at the speaker's head
            m_Tail = UIFactory.Rect("Tail", m_Rect);
            m_Tail.anchorMin = m_Tail.anchorMax = new Vector2(0f, 1f);
            m_Tail.sizeDelta = new Vector2(34, 34);
            m_Tail.localRotation = Quaternion.Euler(0, 0, 45f);
            m_Tail.gameObject.AddComponent<Image>().color = HegraPalette.PanelBg;

            m_Bg = UIFactory.Panel(m_Rect, HegraPalette.PanelBg, "Bg");
            var outline = m_Bg.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(HegraPalette.Lantern.r, HegraPalette.Lantern.g, HegraPalette.Lantern.b, 0.55f);
            outline.effectDistance = new Vector2(3, -3);

            m_Name = UIFactory.Text(m_Rect, "Name", 28, TextAlignmentOptions.Center, HegraPalette.Lantern);
            TopStrip((RectTransform)m_Name.transform, Pad * 0.6f, NameH);
            m_Line = UIFactory.Text(m_Rect, "Line", 34, TextAlignmentOptions.Center);
            m_Line.overflowMode = TextOverflowModes.Overflow;
            m_Line.textWrappingMode = TextWrappingModes.Normal;
            m_Choices = UIFactory.Rect("Choices", m_Rect);
            m_Choices.anchorMin = new Vector2(0, 0); m_Choices.anchorMax = new Vector2(1, 0);
            m_Choices.pivot = new Vector2(0.5f, 0f);
            m_Choices.anchoredPosition = new Vector2(0, Pad * 0.6f);
            m_Choices.sizeDelta = new Vector2(-2 * Pad, ChoiceH);
            UIFactory.HorizontalLayout(m_Choices, 12f);

            m_Blip = gameObject.AddComponent<AudioSource>();
            m_Blip.playOnAwake = false;
            m_Blip.spatialBlend = 0.5f;
            m_Blip.volume = 0.22f;
            m_Blip.clip = MakeBlip();
            MakeOnTop(m_Rect);
            if (HegraAudio.Director) m_Blip.outputAudioMixerGroup = HegraAudio.Director.DialogueGroup;
            m_Canvas.gameObject.SetActive(false);
        }

        static void TopStrip(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -top);
            rt.sizeDelta = new Vector2(-2 * Pad, height);
        }

        void OnEnable()
        {
            Loc.LanguageChanged += Relayout;
            ObjectivePanel.ActionsChanged += OnActionsChanged;
        }

        void OnDisable()
        {
            Loc.LanguageChanged -= Relayout;
            ObjectivePanel.ActionsChanged -= OnActionsChanged;
        }

        public void SetScale(float scale) => m_Scale = Mathf.Clamp(scale, 0.6f, 2f);

        // ───────── dialogue lines ─────────

        public void ShowLine(NpcFigure npc, string speakerName, string line, Color nameColor)
        {
            bool wasVisible = m_Visible && m_Npc == npc;
            if (!m_Visible) HegraAudio.Play2D(Sfx.UiOpen, 0.35f);   // a conversation opens
            m_Npc = npc;
            m_RawName = speakerName;
            m_RawLine = line;
            m_NameColor = nameColor;
            m_ChoiceOnly = false;
            m_Typing = true;
            m_Shown = Time.unscaledTime;
            m_LastBlip = 0;
            Open(!wasVisible);
            Relayout();
            m_Line.maxVisibleCharacters = 0;
        }

        /// <summary>First trigger press while typing: show the whole line at once.</summary>
        public void CompleteTyping()
        {
            if (!m_Typing) return;
            m_Typing = false;
            m_Line.maxVisibleCharacters = int.MaxValue;
        }

        /// <summary>The conversation ended: keep the bubble only if it still offers choices.</summary>
        public void EndDialogue()
        {
            m_RawLine = null;
            m_Typing = false;
            if (!ShowChoicesIfAny()) Close();
        }

        // ───────── objective choices ─────────

        void OnActionsChanged((string key, Action action)[] actions)
        {
            m_Actions = actions;
            var g = GameManager.Instance;
            bool talking = g && g.Dialogue && g.Dialogue.IsPlaying && m_RawLine != null;
            if (talking) { Relayout(); return; }
            if (!ShowChoicesIfAny() && m_ChoiceOnly) Close();
        }

        /// <summary>Shows the objective's choice buttons beside the character the objective is about, if any.</summary>
        bool ShowChoicesIfAny()
        {
            var npc = ChoiceOwner();
            if (npc == null) return false;
            var g = GameManager.Instance;
            var objKey = g && g.State != null ? "obj." + g.State.step : null;
            m_Npc = npc;
            m_RawName = null;
            m_RawLine = objKey != null && Loc.Has(objKey) ? Loc.Get(objKey) : "";
            m_ChoiceOnly = true;
            m_Typing = false;
            Open(!m_Visible);
            Relayout();
            m_Line.maxVisibleCharacters = int.MaxValue;
            return true;
        }

        NpcFigure ChoiceOwner()
        {
            if (m_Actions == null || m_Actions.Length == 0) return null;
            var target = ObjectiveBeacon.Current;
            if (!target) return null;
            var own = target.GetComponentInParent<NpcFigure>();
            if (own && !string.IsNullOrEmpty(own.speakerId)) return own;
            return NpcFigure.NearestSpeaker(target.position, 2.6f);
        }

        // ───────── layout ─────────

        void Relayout()
        {
            if (!m_Canvas) return;
            bool hasName = !string.IsNullOrEmpty(m_RawName);
            m_Name.gameObject.SetActive(hasName);
            if (hasName) { Loc.Apply(m_Name, m_RawName, TextAlignmentOptions.Center); m_Name.color = m_NameColor; }

            Loc.Apply(m_Line, m_RawLine ?? "", TextAlignmentOptions.Center);
            m_Line.fontSize = m_ChoiceOnly ? 30 : 34;
            m_Line.color = m_ChoiceOnly ? HegraPalette.Moonlight : HegraPalette.Text;
            float textW = Width - 2 * Pad;
            float textH = string.IsNullOrEmpty(m_RawLine) ? 0f : Mathf.Ceil(m_Line.GetPreferredValues(m_Line.text, textW, 0f).y) + 6f;

            BuildChoices();
            bool hasChoices = m_Buttons.Count > 0;
            float top = Pad * 0.6f + (hasName ? NameH + 4f : 0f);
            float height = top + textH + (hasChoices ? Gap + ChoiceH : 0f) + Pad * 0.6f;
            m_Rect.sizeDelta = new Vector2(Width, Mathf.Max(90f, height));
            var lrt = (RectTransform)m_Line.transform;
            TopStrip(lrt, top, textH);
            m_Choices.gameObject.SetActive(hasChoices);
            m_Tail.anchoredPosition = new Vector2(2f, -Mathf.Min(70f, m_Rect.sizeDelta.y * 0.5f));
            m_Line.ForceMeshUpdate();
        }

        void BuildChoices()
        {
            foreach (var b in m_Buttons) if (b) Destroy(b);
            m_Buttons.Clear();
            if (m_Actions == null || ChoiceOwner() != m_Npc) return;
            float w = Mathf.Min(280f, (Width - 2 * Pad - 12f * (m_Actions.Length - 1)) / Mathf.Max(1, m_Actions.Length));
            foreach (var a in m_Actions)
            {
                if (a.action == null) continue;
                var act = a.action;
                var b = UIFactory.Button(m_Choices, "Choice_" + a.key, a.key, () => { HegraAudio.Play2D(Sfx.UiConfirm, 0.5f); act(); }, w, ChoiceH - 6f, 24);
                m_Buttons.Add(b.gameObject);
            }
            m_Group.blocksRaycasts = m_Buttons.Count > 0;
            MakeOnTop(m_Choices);
        }

        /// <summary>
        /// Draws the bubble over the world (no depth test), so stall posts, the speaker's body or the player's own
        /// hands can never hide the text.
        /// </summary>
        void MakeOnTop(RectTransform root)
        {
            if (!m_OnTopUi)
            {
                m_OnTopUi = new Material(Graphic.defaultGraphicMaterial) { name = "BubbleUiOnTop" };
                m_OnTopUi.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            }
            foreach (var img in root.GetComponentsInChildren<Image>(true)) img.material = m_OnTopUi;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var m = t.fontMaterial;   // per-text instance
                m.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            }
        }

        /// <summary>The objective's buttons can change after the line started; keep them in sync.</summary>
        void SyncChoices()
        {
            if (Time.unscaledTime < m_NextChoiceCheck) return;
            m_NextChoiceCheck = Time.unscaledTime + 0.3f;
            int want = 0;
            if (m_Actions != null && ChoiceOwner() == m_Npc) foreach (var a in m_Actions) if (a.action != null) want++;
            if (want != m_Buttons.Count) Relayout();
        }

        // ───────── show / hide ─────────

        void Open(bool animate)
        {
            m_Visible = true;
            m_Canvas.gameObject.SetActive(true);
            if (animate) m_Appear = 0f;
        }

        public void Close()
        {
            m_Visible = false;
            m_ChoiceOnly = false;
            m_Typing = false;
            foreach (var b in m_Buttons) if (b) Destroy(b);
            m_Buttons.Clear();
            m_Group.blocksRaycasts = false;
        }

        void LateUpdate()
        {
            if (!m_Canvas) return;
            // fade + scale in/out
            m_Appear = Mathf.MoveTowards(m_Appear, m_Visible ? 1f : 0f, Time.unscaledDeltaTime / 0.18f);
            float ease = Mathf.SmoothStep(0f, 1f, m_Appear);
            m_Group.alpha = ease;
            if (!m_Visible && m_Appear <= 0f) { if (m_Canvas.gameObject.activeSelf) m_Canvas.gameObject.SetActive(false); return; }

            if (m_Typing)
            {
                int total = m_Line.textInfo.characterCount;
                int shown = Mathf.Min(total, Mathf.FloorToInt((Time.unscaledTime - m_Shown) * CharsPerSecond));
                m_Line.maxVisibleCharacters = shown;
                if (shown - m_LastBlip >= 3 && shown < total)
                {
                    m_LastBlip = shown;
                    m_Blip.pitch = UnityEngine.Random.Range(0.9f, 1.12f);
                    m_Blip.Play();
                }
                if (shown >= total) m_Typing = false;
            }

            if (m_Visible) SyncChoices();
            Place(ease);
        }

        void Place(float ease)
        {
            var g = GameManager.Instance;
            if (!g || !g.Rig || !m_Npc) return;
            var eye = g.Rig.Head.position;
            var head = m_Npc.DialogueAnchorPosition;
            var to = head - eye; to.y = 0f;
            float dist = Mathf.Max(0.3f, to.magnitude);
            var fwd = to / dist;
            var right = Vector3.Cross(Vector3.up, fwd);
            // constant apparent size: grows with distance beyond ~1.25 m, never smaller than its near size
            float k = UIFactory.MetersPerPixel * Mathf.Clamp(dist / 1.25f, 1f, 4.5f) * m_Scale;
            float halfW = Width * 0.5f * k;
            float h = m_Rect.sizeDelta.y * k;
            // beside the head on the player's right, pulled a little toward the player so the body never covers it
            var pos = head + right * (0.16f + halfW) - fwd * Mathf.Min(0.35f, dist * 0.25f) + Vector3.up * (0.06f + h * 0.15f);
            var face = pos - eye; face.y = 0f;
            transform.SetPositionAndRotation(pos, face.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(face) : Quaternion.identity);
            m_Rect.localScale = Vector3.one * k * Mathf.Lerp(0.85f, 1f, ease);
        }

        static AudioClip MakeBlip()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.045f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 70f) * Mathf.Min(1f, t * 900f);
                data[i] = env * (Mathf.Sin(2f * Mathf.PI * 760f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * 1520f * t) * 0.18f);
            }
            var clip = AudioClip.Create("DialogueBlip", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
