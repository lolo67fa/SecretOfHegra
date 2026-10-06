using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// World-space VR keyboard for the player's name: Arabic and English layouts, space, delete, clear,
    /// and quick presets "Lulu" / "لولو". Works with XR rays (trigger) and the mouse in the simulator.
    /// </summary>
    public class NameKeyboard : MonoBehaviour
    {
        static readonly string[][] Arabic =
        {
            new[] { "ض", "ص", "ث", "ق", "ف", "غ", "ع", "ه", "خ", "ح", "ج" },
            new[] { "ش", "س", "ي", "ب", "ل", "ا", "ت", "ن", "م", "ك", "ط" },
            new[] { "ذ", "ء", "أ", "إ", "ر", "ى", "ة", "و", "ز", "ظ", "د" },
        };
        static readonly string[][] English =
        {
            new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
            new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l" },
            new[] { "z", "x", "c", "v", "b", "n", "m", "-" },
        };

        Canvas m_Canvas;
        TextMeshProUGUI m_Field;
        RectTransform m_KeyArea;
        readonly List<GameObject> m_Keys = new List<GameObject>();
        bool m_ArabicLayout;
        string m_Value = "";
        Action<string> m_OnConfirm;

        public bool IsOpen => m_Canvas && m_Canvas.gameObject.activeSelf;
        public string Value => m_Value;

        public static NameKeyboard Create(Transform parent, Camera cam)
        {
            var root = new GameObject("NameKeyboard");
            root.transform.SetParent(parent, false);
            var kb = root.AddComponent<NameKeyboard>();
            kb.Build(cam);
            kb.m_Canvas.gameObject.SetActive(false);
            return kb;
        }

        void Build(Camera cam)
        {
            m_Canvas = UIFactory.CreateCanvas("KeyboardCanvas", transform, new Vector2(900, 560), cam, 15);
            var rt = (RectTransform)m_Canvas.transform;
            UIFactory.Panel(rt, HegraPalette.PanelBg, "Bg");
            var content = UIFactory.Rect("Content", rt);
            UIFactory.Stretch(content, 16);
            UIFactory.VerticalLayout(content, 8, 0);

            var title = UIFactory.LayoutText(content, "Title", 860, 44, 26, TextAlignmentOptions.Center);
            title.color = HegraPalette.Lantern;
            title.gameObject.AddComponent<LocalizedText>().SetKey("kb.title", TextAlignmentOptions.Center);

            var fieldRow = UIFactory.Row(content, "FieldRow", 70, 0, 860);
            var fieldBg = UIFactory.Rect("FieldBg", fieldRow);
            fieldBg.sizeDelta = new Vector2(700, 64);
            fieldBg.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 700;
            fieldBg.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.93f, 0.88f, 0.8f, 1f);
            m_Field = UIFactory.Text(fieldBg, "Field", 40, TextAlignmentOptions.Center, new Color(0.2f, 0.12f, 0.1f));
            UIFactory.Stretch((RectTransform)m_Field.transform, 6);

            m_KeyArea = UIFactory.Rect("Keys", content);
            m_KeyArea.sizeDelta = new Vector2(860, 240);
            m_KeyArea.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 240;
            UIFactory.VerticalLayout(m_KeyArea, 6, 0);

            var bottom = UIFactory.Row(content, "Bottom", 64, 8, 860);
            UIFactory.Button(bottom, "Layout", "kb.layout", () => { m_ArabicLayout = !m_ArabicLayout; BuildKeys(); }, 130, 60, 22);
            UIFactory.Button(bottom, "Space", "kb.space", () => Type(" "), 130, 60, 22);
            UIFactory.Button(bottom, "Delete", "kb.delete", Backspace, 110, 60, 22);
            UIFactory.Button(bottom, "Clear", "kb.clear", () => { m_Value = ""; RefreshField(); }, 100, 60, 22);
            UIFactory.RawButton(bottom, "PresetLulu", "Lulu", () => { m_Value = "Lulu"; RefreshField(); }, 100, 60, 24);
            UIFactory.RawButton(bottom, "PresetLuluAr", "لولو", () => { m_Value = "لولو"; RefreshField(); }, 100, 60, 26);
            UIFactory.Button(bottom, "Confirm", "kb.confirm", Confirm, 140, 60, 24);
        }

        void BuildKeys()
        {
            foreach (var k in m_Keys) if (k) Destroy(k);
            m_Keys.Clear();
            var rows = m_ArabicLayout ? Arabic : English;
            foreach (var row in rows)
            {
                var r = UIFactory.Row(m_KeyArea, "KeyRow", 72, 6, 860);
                // Arabic keyboards start at the right edge
                r.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().reverseArrangement = m_ArabicLayout;
                m_Keys.Add(r.gameObject);
                foreach (var key in row)
                {
                    var k = key;
                    UIFactory.RawButton(r, "Key_" + k, k, () => Type(k), 70, 70, 34);
                }
            }
        }

        public void Open(Transform anchor, string initial, Action<string> onConfirm)
        {
            m_OnConfirm = onConfirm;
            m_Value = initial ?? "";
            m_ArabicLayout = Loc.IsArabic;
            if (anchor) transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            m_Canvas.gameObject.SetActive(true);
            BuildKeys();
            RefreshField();
        }

        public void Close() => m_Canvas.gameObject.SetActive(false);

        void Type(string s)
        {
            if (m_Value.Length >= NameRules.MaxLength) { HegraAudio.Play2D(Sfx.Fail, 0.5f); return; }
            if (s == " " && (m_Value.Length == 0 || m_Value.EndsWith(" "))) return;
            if (!m_ArabicLayout && (m_Value.Length == 0 || m_Value.EndsWith(" "))) s = s.ToUpperInvariant();
            m_Value += s;
            RefreshField();
        }

        void Backspace()
        {
            if (m_Value.Length == 0) return;
            m_Value = m_Value.Substring(0, m_Value.Length - 1);
            RefreshField();
        }

        void RefreshField()
        {
            if (string.IsNullOrEmpty(m_Value))
            {
                Loc.Apply(m_Field, Loc.Get("kb.placeholder"), TextAlignmentOptions.Center);
                m_Field.alpha = 0.45f;
            }
            else
            {
                Loc.Apply(m_Field, m_Value, TextAlignmentOptions.Center);
                m_Field.alpha = 1f;
            }
        }

        void Confirm()
        {
            var v = m_Value.Trim();
            if (v.Length == 0) { HegraAudio.Play2D(Sfx.Fail, 0.5f); return; }
            Close();
            m_OnConfirm?.Invoke(v);
        }

        /// <summary>Automation/accessibility hook: confirm a given name without typing.</summary>
        public void SubmitForTest(string name)
        {
            m_Value = name;
            Confirm();
        }
    }
}
