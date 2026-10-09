using TMPro;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Subtitle bar (speaker + line), scaled by the subtitle-size setting. It is placed in front of the player when a
    /// conversation starts and stays there until it ends (it does not chase the head).
    /// </summary>
    public class SubtitleDisplay : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_Speaker;
        [SerializeField] TextMeshProUGUI m_Line;
        [SerializeField] CanvasGroup m_Group;

        string m_RawSpeaker;
        string m_RawLine;
        Color m_SpeakerColor = Color.white;
        float m_BaseScale = UIFactory.MetersPerPixel;
        LazyFollow m_Follow;

        public static SubtitleDisplay Create(Transform head, Camera cam)
        {
            var root = new GameObject("Subtitles");
            var follow = root.AddComponent<LazyFollow>();
            follow.target = head;
            follow.distance = 1.9f;
            follow.heightOffset = -0.45f;
            follow.stayPut = true;
            follow.yawThreshold = 25f;

            var canvas = UIFactory.CreateCanvas("SubtitleCanvas", root.transform, new Vector2(1100, 230), cam, 10);
            var rt = (RectTransform)canvas.transform;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            UIFactory.Panel(rt, new Color(0.03f, 0.03f, 0.08f, 0.78f), "Bg");

            var speaker = UIFactory.Text(rt, "Speaker", 30, TextAlignmentOptions.Top, HegraPalette.Lantern);
            var srt = (RectTransform)speaker.transform;
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(-40, 46); srt.anchoredPosition = new Vector2(0, -8);

            var line = UIFactory.Text(rt, "Line", 38, TextAlignmentOptions.Center);
            var lrt = (RectTransform)line.transform;
            UIFactory.Stretch(lrt, 24);
            lrt.offsetMax = new Vector2(-28, -54);

            var display = root.AddComponent<SubtitleDisplay>();
            display.m_Speaker = speaker;
            display.m_Line = line;
            display.m_Group = group;
            display.m_Follow = follow;
            display.m_BaseScale = UIFactory.MetersPerPixel;
            group.alpha = 0f;
            return display;
        }

        void OnEnable() => Loc.LanguageChanged += Refresh;
        void OnDisable() => Loc.LanguageChanged -= Refresh;

        public void SetScale(float scale)
        {
            if (m_Line) m_Line.canvas.transform.localScale = Vector3.one * (m_BaseScale * scale);
        }

        public void Show(string speakerName, string line, Color speakerColor)
        {
            m_RawSpeaker = speakerName;
            m_RawLine = line;
            m_SpeakerColor = speakerColor;
            bool starting = !m_Group || m_Group.alpha < 0.01f;
            if (starting && m_Follow) m_Follow.SnapNow();
            Refresh();
            if (m_Group) m_Group.alpha = 1f;
        }

        public void Hide()
        {
            m_RawLine = null;
            if (m_Group) m_Group.alpha = 0f;
        }

        void Refresh()
        {
            if (m_RawLine == null) return;
            Loc.Apply(m_Speaker, m_RawSpeaker ?? "", TextAlignmentOptions.Top);
            m_Speaker.color = m_SpeakerColor;
            Loc.Apply(m_Line, m_RawLine, TextAlignmentOptions.Center);
        }
    }
}
