using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecretOfHegra
{
    /// <summary>
    /// Compact objective card, placed once in front of the player (a little to the left and a bit further away) when a
    /// new objective starts, then left in the world so it never chases the player around. Shows the
    /// current objective, the controller hint for it, the current progressive hint, and up to three
    /// accessible action buttons (skip, "do it for me", etc.).
    /// </summary>
    public class ObjectivePanel : MonoBehaviour
    {
        [SerializeField] LocalizedText m_Objective;
        [SerializeField] TextMeshProUGUI m_Controls;
        [SerializeField] LocalizedText m_Hint;
        [SerializeField] RectTransform m_ButtonRow;
        [SerializeField] CanvasGroup m_Group;

        readonly List<GameObject> m_Buttons = new List<GameObject>();
        string m_ControlsKey;
        Func<string> m_ControlsResolver;
        LazyFollow m_Follow;
        Transform m_Canvas;
        Vector3 m_CanvasPos;
        Quaternion m_CanvasRot;
        Transform m_WorldAnchor;
        string m_ObjectiveKey;

        /// <summary>Raised whenever the action buttons change (null when cleared); the speech bubble mirrors them.</summary>
        public static event Action<(string key, Action action)[]> ActionsChanged;

        public static ObjectivePanel Create(Transform head, Camera cam)
        {
            var root = new GameObject("ObjectivePanel");
            var follow = root.AddComponent<LazyFollow>();
            follow.target = head;
            follow.distance = 1.7f;
            follow.heightOffset = -0.12f;
            follow.stayPut = true;
            follow.yawThreshold = 40f;
            follow.followSpeed = 2f;

            var canvas = UIFactory.CreateCanvas("ObjectiveCanvas", root.transform, new Vector2(640, 330), cam, 5);
            canvas.transform.localPosition = new Vector3(-0.8f, 0f, 0f);
            canvas.transform.localRotation = Quaternion.Euler(0f, -28f, 0f);
            var rt = (RectTransform)canvas.transform;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            UIFactory.Panel(rt, HegraPalette.PanelBg, "Bg");
            var content = UIFactory.Rect("Content", rt);
            UIFactory.Stretch(content, 14);
            UIFactory.VerticalLayout(content, 6, 0);

            var p = root.AddComponent<ObjectivePanel>();
            p.m_Follow = follow;
            p.m_Canvas = canvas.transform;
            p.m_CanvasPos = canvas.transform.localPosition;
            p.m_CanvasRot = canvas.transform.localRotation;
            var obj = UIFactory.LayoutText(content, "Objective", 612, 96, 30, TextAlignmentOptions.Center);
            p.m_Objective = obj.gameObject.AddComponent<LocalizedText>();
            p.m_Controls = UIFactory.LayoutText(content, "Controls", 612, 60, 22, TextAlignmentOptions.Center);
            p.m_Controls.color = HegraPalette.Lantern;
            var hint = UIFactory.LayoutText(content, "Hint", 612, 66, 22, TextAlignmentOptions.Center);
            hint.color = HegraPalette.Moonlight;
            p.m_Hint = hint.gameObject.AddComponent<LocalizedText>();
            p.m_ButtonRow = UIFactory.Row(content, "Buttons", 64, 10, 612);
            p.m_Group = group;
            group.alpha = 0f;
            return p;
        }

        void OnEnable() => Loc.LanguageChanged += RefreshControls;
        void OnDisable() => Loc.LanguageChanged -= RefreshControls;

        public void Show(string objectiveKey, string controlsKey = null, Func<string> controlsResolver = null)
        {
            ClearButtons();
            if (m_Group) { m_Group.alpha = 1f; m_Group.blocksRaycasts = true; }
            if (string.IsNullOrEmpty(objectiveKey)) { Hide(); return; }
            // a new objective (start of a stage) is placed in front of the player once; it does not follow afterwards
            if (objectiveKey != m_ObjectiveKey) BringIntoView();
            m_ObjectiveKey = objectiveKey;
            m_Objective.SetKey(objectiveKey, TextAlignmentOptions.Center);
            m_ControlsKey = controlsKey;
            m_ControlsResolver = controlsResolver;
            RefreshControls();
            ClearHint();
        }

        /// <summary>
        /// Pins the card to a world point (e.g. right beside the clay lamp in the first lesson) instead of following
        /// the head; null returns it to the usual lazy head-follow on the left.
        /// </summary>
        public void SetWorldAnchor(Transform anchor)
        {
            if (anchor == m_WorldAnchor) return;
            m_WorldAnchor = anchor;
            if (m_Follow) m_Follow.enabled = !anchor;
            if (anchor)
            {
                transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                if (m_Canvas) { m_Canvas.localPosition = Vector3.zero; m_Canvas.localRotation = Quaternion.identity; }
            }
            else
            {
                if (m_Canvas) { m_Canvas.localPosition = m_CanvasPos; m_Canvas.localRotation = m_CanvasRot; }
                if (m_Follow) m_Follow.SnapNow();
            }
        }

        /// <summary>Places the card in front of the player again (new objective, or a hint the player asked for).</summary>
        public void BringIntoView()
        {
            if (!m_WorldAnchor && m_Follow) m_Follow.SnapNow();
        }

        public void Hide()
        {
            m_ObjectiveKey = null;
            if (m_Group) { m_Group.alpha = 0f; m_Group.blocksRaycasts = false; }
            ClearButtons();
        }

        public void ShowHint(string key)
        {
            if (string.IsNullOrEmpty(key)) { ClearHint(); return; }
            m_Hint.gameObject.SetActive(true);
            m_Hint.SetKey(key, TextAlignmentOptions.Center);
            BringIntoView();   // hints are only shown when the player asks for one
        }

        public void ClearHint()
        {
            if (m_Hint) m_Hint.gameObject.SetActive(false);
        }

        void RefreshControls()
        {
            if (!m_Controls) return;
            if (string.IsNullOrEmpty(m_ControlsKey)) { m_Controls.text = ""; return; }
            string raw = Loc.Get(m_ControlsKey);
            if (m_ControlsResolver != null)
            {
                try { raw = string.Format(raw, m_ControlsResolver()); } catch (FormatException) { }
            }
            Loc.Apply(m_Controls, raw, TextAlignmentOptions.Center);
        }

        public void SetActions(params (string key, Action action)[] actions)
        {
            ClearButtons();
            if (actions == null) return;
            ActionsChanged?.Invoke(actions);
            foreach (var a in actions)
            {
                if (a.action == null) continue;
                var b = UIFactory.Button(m_ButtonRow, "Action_" + a.key, a.key, a.action, 190, 60, 22);
                m_Buttons.Add(b.gameObject);
            }
        }

        public void ClearButtons()
        {
            bool had = m_Buttons.Count > 0;
            foreach (var b in m_Buttons) if (b) Destroy(b);
            m_Buttons.Clear();
            if (had) ActionsChanged?.Invoke(null);
        }
    }
}
