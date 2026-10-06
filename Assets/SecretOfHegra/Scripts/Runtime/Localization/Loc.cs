using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Arabic/English string tables loaded from JSON (Data/ui_strings.json and Data/dialogue.json),
    /// plus helpers that apply a string to a TextMesh Pro component with correct Arabic shaping and
    /// right-to-left layout. All player-facing text goes through here — no dialogue is hard-coded.
    /// </summary>
    public static class Loc
    {
        [Serializable] public class Entry { public string key; public string ar; public string en; }
        [Serializable] class Table { public Entry[] entries; }

        static readonly Dictionary<string, Entry> s_Entries = new Dictionary<string, Entry>();
        static readonly Dictionary<string, string> s_ShapeCache = new Dictionary<string, string>();

        public static Language Current { get; private set; } = Language.Arabic;
        public static event Action LanguageChanged;
        public static TMP_FontAsset Font { get; set; }

        public static bool IsArabic => Current == Language.Arabic;

        public static void LoadTable(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var table = JsonUtility.FromJson<Table>(json);
            if (table?.entries == null) return;
            foreach (var e in table.entries)
                if (!string.IsNullOrEmpty(e.key))
                    s_Entries[e.key] = e;
        }

        public static void Clear()
        {
            s_Entries.Clear();
            s_ShapeCache.Clear();
        }

        public static void SetLanguage(Language language)
        {
            if (Current == language) return;
            Current = language;
            LanguageChanged?.Invoke();
        }

        public static bool Has(string key) => !string.IsNullOrEmpty(key) && s_Entries.ContainsKey(key);

        /// <summary>Raw (unshaped) string in the current language. Falls back to the other language, then the key.</summary>
        public static string Get(string key) => Get(key, Current);

        public static string Get(string key, Language language)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (!s_Entries.TryGetValue(key, out var e)) return key;
            var s = language == Language.Arabic ? e.ar : e.en;
            if (string.IsNullOrEmpty(s)) s = language == Language.Arabic ? e.en : e.ar;
            return s ?? key;
        }

        public static string Format(string key, params object[] args)
        {
            try { return string.Format(Get(key), args); }
            catch (FormatException) { return Get(key); }
        }

        /// <summary>Shapes Arabic text for TMP RTL mode (cached).</summary>
        public static string Shape(string raw)
        {
            if (string.IsNullOrEmpty(raw) || !ArabicShaper.ContainsArabic(raw)) return raw ?? "";
            if (s_ShapeCache.TryGetValue(raw, out var cached)) return cached;
            var shaped = ArabicShaper.ShapeForRtl(raw);
            if (s_ShapeCache.Count > 512) s_ShapeCache.Clear();
            s_ShapeCache[raw] = shaped;
            return shaped;
        }

        /// <summary>
        /// Writes a raw string into a TMP text, setting RTL mode and mirrored alignment when the string
        /// contains Arabic. <paramref name="baseAlignment"/> is the alignment used for left-to-right text.
        /// </summary>
        public static void Apply(TMP_Text text, string raw, TextAlignmentOptions baseAlignment = TextAlignmentOptions.Center)
        {
            if (text == null) return;
            if (Font != null && text.font != Font)
                text.font = Font;
            bool rtl = ArabicShaper.ContainsArabic(raw);
            text.isRightToLeftText = rtl;
            text.alignment = rtl ? Mirror(baseAlignment) : baseAlignment;
            text.text = rtl ? Shape(raw) : (raw ?? "");
        }

        public static void ApplyKey(TMP_Text text, string key, TextAlignmentOptions baseAlignment = TextAlignmentOptions.Center)
        {
            Apply(text, Get(key), baseAlignment);
        }

        public static TextAlignmentOptions Mirror(TextAlignmentOptions a)
        {
            switch (a)
            {
                case TextAlignmentOptions.Left: return TextAlignmentOptions.Right;
                case TextAlignmentOptions.TopLeft: return TextAlignmentOptions.TopRight;
                case TextAlignmentOptions.BottomLeft: return TextAlignmentOptions.BottomRight;
                case TextAlignmentOptions.Right: return TextAlignmentOptions.Left;
                case TextAlignmentOptions.TopRight: return TextAlignmentOptions.TopLeft;
                case TextAlignmentOptions.BottomRight: return TextAlignmentOptions.BottomLeft;
                default: return a;
            }
        }
    }
}
