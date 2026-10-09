using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SecretOfHegra
{
    /// <summary>
    /// Builds world-space uGUI panels that work with XR rays (TrackedDeviceGraphicRaycaster) and with the
    /// mouse in the desktop simulator. One canvas pixel = 1 mm by default.
    /// </summary>
    public static class UIFactory
    {
        public const float MetersPerPixel = 0.001f;

        public static Canvas CreateCanvas(string name, Transform parent, Vector2 sizePx, Camera eventCamera, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eventCamera;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 3f;
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizePx;
            rt.localScale = Vector3.one * MetersPerPixel;
            return canvas;
        }

        public static RectTransform Rect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        public static Image Panel(RectTransform parent, Color color, string name = "Panel")
        {
            var rt = Rect(name, parent);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Text(RectTransform parent, string name, float fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Loc.Font) t.font = Loc.Font;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color ?? HegraPalette.Text;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static LocalizedText LocalizedLabel(RectTransform parent, string name, string key, float fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            var t = Text(parent, name, fontSize, alignment, color);
            var loc = t.gameObject.AddComponent<LocalizedText>();
            loc.SetKey(key, alignment);
            return loc;
        }

        public static Button Button(RectTransform parent, string name, string key, Action onClick, float width = 360f, float height = 72f, float fontSize = 30f)
        {
            var rt = Rect(name, parent);
            rt.sizeDelta = new Vector2(width, height);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = HegraPalette.ButtonBg;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.25f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.6f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1.5f;
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null)
                btn.onClick.AddListener(() =>
                {
                    HegraAudio.PlayUiClick();
                    Haptics.Both(0.25f, 0.05f);
                    onClick();
                });

            var label = LocalizedLabel(rt, "Label", key, fontSize, TextAlignmentOptions.Center);
            Stretch((RectTransform)label.transform, 6f);
            return btn;
        }

        /// <summary>Button whose label is a literal (keyboard keys, names) rather than a localization key.</summary>
        public static Button RawButton(RectTransform parent, string name, string rawLabel, Action onClick, float width, float height, float fontSize)
        {
            var b = Button(parent, name, "ui.empty", onClick, width, height, fontSize);
            var loc = b.GetComponentInChildren<LocalizedText>();
            var t = loc.GetComponent<TMP_Text>();
            UnityEngine.Object.Destroy(loc);
            Loc.Apply(t, rawLabel, TextAlignmentOptions.Center);
            return b;
        }

        public static VerticalLayoutGroup VerticalLayout(RectTransform rt, float spacing, int padding, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childAlignment = align;
            v.childControlHeight = false;
            v.childControlWidth = false;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = false;
            return v;
        }

        public static HorizontalLayoutGroup HorizontalLayout(RectTransform rt, float spacing, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlHeight = false;
            h.childControlWidth = false;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;
            return h;
        }

        public static RectTransform Row(RectTransform parent, string name, float height, float spacing = 12f, float width = -1f)
        {
            var rt = Rect(name, parent);
            if (width <= 0f) width = parent.rect.width > 1f ? parent.rect.width : 600f;
            rt.sizeDelta = new Vector2(width, height);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.preferredWidth = width;
            HorizontalLayout(rt, spacing);
            return rt;
        }

        public static TextMeshProUGUI LayoutText(RectTransform parent, string name, float width, float height, float fontSize, TextAlignmentOptions align)
        {
            var t = Text(parent, name, fontSize, align);
            var rt = (RectTransform)t.transform;
            rt.sizeDelta = new Vector2(width, height);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            return t;
        }
    }
}
