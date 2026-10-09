using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SecretOfHegra
{
    /// <summary>
    /// Large, elegant titles placed once in front of the player: the opening title over black ("سر الحِجر") and a
    /// chapter card each time the journey reaches a new place. Drawn above everything, including the screen fade.
    /// </summary>
    public class TitleCard : MonoBehaviour
    {
        Canvas m_Canvas;
        CanvasGroup m_Group;
        TextMeshProUGUI m_Title, m_Sub;
        Image m_Line;
        Coroutine m_Routine;

        public static TitleCard Create(Camera cam)
        {
            var root = new GameObject("TitleCard");
            var t = root.AddComponent<TitleCard>();
            t.Build(cam);
            return t;
        }

        void Build(Camera cam)
        {
            m_Canvas = UIFactory.CreateCanvas("TitleCanvas", transform, new Vector2(1400, 420), cam, 50);
            var rt = (RectTransform)m_Canvas.transform;
            m_Group = m_Canvas.gameObject.AddComponent<CanvasGroup>();
            m_Group.alpha = 0f;
            m_Group.blocksRaycasts = false;
            m_Title = UIFactory.Text(rt, "Title", 120, TextAlignmentOptions.Center, HegraPalette.Lantern);
            Place((RectTransform)m_Title.transform, 60f, 180f);
            m_Line = UIFactory.Rect("Line", rt).gameObject.AddComponent<Image>();
            m_Line.color = new Color(HegraPalette.Lantern.r, HegraPalette.Lantern.g, HegraPalette.Lantern.b, 0.7f);
            var lrt = m_Line.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(520, 3);
            lrt.anchoredPosition = new Vector2(0, -40);
            m_Sub = UIFactory.Text(rt, "Subtitle", 44, TextAlignmentOptions.Center, HegraPalette.Moonlight);
            Place((RectTransform)m_Sub.transform, -100f, 90f);

            // above the world and above the black screen fade (Overlay+100)
            var ui = new Material(Graphic.defaultGraphicMaterial) { renderQueue = 4200 };
            ui.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            m_Line.material = ui;
            foreach (var t in new[] { m_Title, m_Sub })
            {
                var m = t.fontMaterial;
                m.renderQueue = 4200;
                m.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            }
            m_Canvas.gameObject.SetActive(false);
        }

        static void Place(RectTransform rt, float y, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1360, h);
            rt.anchoredPosition = new Vector2(0, y);
        }

        public bool IsShowing => m_Routine != null;

        /// <summary>Shows the card in front of the head: fade in, hold, fade out.</summary>
        public void Show(string title, string subtitle, float hold = 2.4f, float distance = 2.6f, float scale = 1f)
        {
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = StartCoroutine(Run(title, subtitle, hold, distance, scale));
        }

        IEnumerator Run(string title, string subtitle, float hold, float distance, float scale)
        {
            var gm = GameManager.Instance;
            var head = gm && gm.Rig ? gm.Rig.Head : Camera.main ? Camera.main.transform : null;
            if (head)
            {
                var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
                fwd.Normalize();
                var pos = head.position + fwd * distance + Vector3.up * 0.12f;
                // in the very first frames the headset may not report its height yet: assume standing eye level
                float floor = gm && gm.Rig ? gm.Rig.transform.position.y : 0f;
                pos.y = Mathf.Max(pos.y, floor + 1.55f);
                transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd));
            }
            // big, cinematic letters: about 9° tall for the title at any distance
            m_Canvas.transform.localScale = Vector3.one * 0.0032f * (distance / 2.6f) * scale;
            Loc.Apply(m_Title, title, TextAlignmentOptions.Center);
            Loc.Apply(m_Sub, subtitle ?? "", TextAlignmentOptions.Center);
            m_Sub.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            m_Line.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            m_Canvas.gameObject.SetActive(true);
            yield return Fade(0f, 1f, 0.9f);
            for (float t = 0; t < hold; t += Time.unscaledDeltaTime) yield return null;
            yield return Fade(1f, 0f, 1.0f);
            m_Canvas.gameObject.SetActive(false);
            m_Routine = null;
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                m_Group.alpha = Mathf.Lerp(from, to, k);
                // the title drifts up a few millimetres while appearing: a little life
                m_Title.rectTransform.anchoredPosition = new Vector2(0, 60f + (1f - m_Group.alpha) * -14f);
                yield return null;
            }
            m_Group.alpha = to;
        }
    }
}
