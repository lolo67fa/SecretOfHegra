using System.Collections.Generic;
using System.Text;

namespace SecretOfHegra
{
    /// <summary>
    /// Minimal Arabic shaping for TextMesh Pro, which has no OpenType shaping. Converts letters to their
    /// contextual Presentation Forms-B glyphs (isolated/final/initial/medial), forms lam-alef ligatures,
    /// and prepares the string for TMP's right-to-left mode:
    ///  • output stays in logical order (TMP RTL lays each wrapped line right-to-left),
    ///  • embedded left-to-right runs (Latin, digits) are pre-reversed so they read correctly,
    ///  • combining marks stay after their base letter (verified to render on the base in TMP RTL),
    ///  • rich-text tags are left untouched.
    /// </summary>
    public static class ArabicShaper
    {
        // isolated, final, initial, medial (0 = form does not exist)
        static readonly Dictionary<char, char[]> s_Forms = new Dictionary<char, char[]>
        {
            { 'ء', new[] { 'ﺀ', '\0', '\0', '\0' } },
            { 'آ', new[] { 'ﺁ', 'ﺂ', '\0', '\0' } },
            { 'أ', new[] { 'ﺃ', 'ﺄ', '\0', '\0' } },
            { 'ؤ', new[] { 'ﺅ', 'ﺆ', '\0', '\0' } },
            { 'إ', new[] { 'ﺇ', 'ﺈ', '\0', '\0' } },
            { 'ئ', new[] { 'ﺉ', 'ﺊ', 'ﺋ', 'ﺌ' } },
            { 'ا', new[] { 'ﺍ', 'ﺎ', '\0', '\0' } },
            { 'ب', new[] { 'ﺏ', 'ﺐ', 'ﺑ', 'ﺒ' } },
            { 'ة', new[] { 'ﺓ', 'ﺔ', '\0', '\0' } },
            { 'ت', new[] { 'ﺕ', 'ﺖ', 'ﺗ', 'ﺘ' } },
            { 'ث', new[] { 'ﺙ', 'ﺚ', 'ﺛ', 'ﺜ' } },
            { 'ج', new[] { 'ﺝ', 'ﺞ', 'ﺟ', 'ﺠ' } },
            { 'ح', new[] { 'ﺡ', 'ﺢ', 'ﺣ', 'ﺤ' } },
            { 'خ', new[] { 'ﺥ', 'ﺦ', 'ﺧ', 'ﺨ' } },
            { 'د', new[] { 'ﺩ', 'ﺪ', '\0', '\0' } },
            { 'ذ', new[] { 'ﺫ', 'ﺬ', '\0', '\0' } },
            { 'ر', new[] { 'ﺭ', 'ﺮ', '\0', '\0' } },
            { 'ز', new[] { 'ﺯ', 'ﺰ', '\0', '\0' } },
            { 'س', new[] { 'ﺱ', 'ﺲ', 'ﺳ', 'ﺴ' } },
            { 'ش', new[] { 'ﺵ', 'ﺶ', 'ﺷ', 'ﺸ' } },
            { 'ص', new[] { 'ﺹ', 'ﺺ', 'ﺻ', 'ﺼ' } },
            { 'ض', new[] { 'ﺽ', 'ﺾ', 'ﺿ', 'ﻀ' } },
            { 'ط', new[] { 'ﻁ', 'ﻂ', 'ﻃ', 'ﻄ' } },
            { 'ظ', new[] { 'ﻅ', 'ﻆ', 'ﻇ', 'ﻈ' } },
            { 'ع', new[] { 'ﻉ', 'ﻊ', 'ﻋ', 'ﻌ' } },
            { 'غ', new[] { 'ﻍ', 'ﻎ', 'ﻏ', 'ﻐ' } },
            { 'ف', new[] { 'ﻑ', 'ﻒ', 'ﻓ', 'ﻔ' } },
            { 'ق', new[] { 'ﻕ', 'ﻖ', 'ﻗ', 'ﻘ' } },
            { 'ك', new[] { 'ﻙ', 'ﻚ', 'ﻛ', 'ﻜ' } },
            { 'ل', new[] { 'ﻝ', 'ﻞ', 'ﻟ', 'ﻠ' } },
            { 'م', new[] { 'ﻡ', 'ﻢ', 'ﻣ', 'ﻤ' } },
            { 'ن', new[] { 'ﻥ', 'ﻦ', 'ﻧ', 'ﻨ' } },
            { 'ه', new[] { 'ﻩ', 'ﻪ', 'ﻫ', 'ﻬ' } },
            { 'و', new[] { 'ﻭ', 'ﻮ', '\0', '\0' } },
            { 'ى', new[] { 'ﻯ', 'ﻰ', '\0', '\0' } },
            { 'ي', new[] { 'ﻱ', 'ﻲ', 'ﻳ', 'ﻴ' } },
            { 'پ', new[] { 'ﭖ', 'ﭗ', 'ﭘ', 'ﭙ' } },
            { 'چ', new[] { 'ﭺ', 'ﭻ', 'ﭼ', 'ﭽ' } },
            { 'ژ', new[] { 'ﮊ', 'ﮋ', '\0', '\0' } },
            { 'ک', new[] { 'ﮎ', 'ﮏ', 'ﮐ', 'ﮑ' } },
            { 'گ', new[] { 'ﮒ', 'ﮓ', 'ﮔ', 'ﮕ' } },
            { 'ی', new[] { 'ﯼ', 'ﯽ', 'ﯾ', 'ﯿ' } },
        };

        const char Tatweel = 'ـ';
        const char Lam = 'ل';

        public static bool IsArabicLetter(char c) => s_Forms.ContainsKey(c);

        public static bool IsTransparent(char c) =>
            (c >= 'ً' && c <= 'ٟ') || c == 'ٰ' || (c >= 'ؐ' && c <= 'ؚ') || (c >= 'ۖ' && c <= 'ۭ');

        static bool JoinsBoth(char c)
        {
            if (c == Tatweel) return true;
            return s_Forms.TryGetValue(c, out var f) && f[2] != '\0';
        }

        static bool JoinsPrevious(char c) => c == Tatweel || (s_Forms.ContainsKey(c) && c != 'ء');

        static char LamAlef(char alef, bool final)
        {
            switch (alef)
            {
                case 'آ': return final ? 'ﻶ' : 'ﻵ';
                case 'أ': return final ? 'ﻸ' : 'ﻷ';
                case 'إ': return final ? 'ﻺ' : 'ﻹ';
                case 'ا': return final ? 'ﻼ' : 'ﻻ';
                default: return '\0';
            }
        }

        public static bool ContainsArabic(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (var c in s)
                if ((c >= '؀' && c <= 'ۿ') || (c >= 'ﭐ' && c <= 'ﻼ'))
                    return true;
            return false;
        }

        /// <summary>Shapes Arabic and prepares it for a TMP text with isRightToLeftText = true.</summary>
        public static string ShapeForRtl(string input, bool keepMarks = true)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? "";
            var shaped = Shape(input, keepMarks);
            return ReverseLtrRuns(shaped);
        }

        /// <summary>Contextual shaping only (logical order preserved).</summary>
        public static string Shape(string input, bool keepMarks = true)
        {
            var src = input;
            var sb = new StringBuilder(src.Length);
            int n = src.Length;
            for (int i = 0; i < n; i++)
            {
                char c = src[i];
                if (IsTransparent(c))
                    continue; // marks are emitted together with their base letter below

                // collect marks that follow this character
                int markStart = i + 1, markEnd = i + 1;
                while (markEnd < n && IsTransparent(src[markEnd])) markEnd++;

                if (!s_Forms.ContainsKey(c))
                {
                    sb.Append(c);
                    if (keepMarks) AppendMarks(sb, src, markStart, markEnd);
                    i = markEnd - 1;
                    continue;
                }

                char prev = PrevBase(src, i);
                bool joinPrev = prev != '\0' && JoinsBoth(prev) && JoinsPrevious(c);

                // lam-alef ligature
                if (c == Lam && markEnd < n)
                {
                    char lig = LamAlef(src[markEnd], joinPrev);
                    if (lig != '\0')
                    {
                        int alefMarksEnd = markEnd + 1;
                        while (alefMarksEnd < n && IsTransparent(src[alefMarksEnd])) alefMarksEnd++;
                        sb.Append(lig);
                        if (keepMarks)
                        {
                            AppendMarks(sb, src, markStart, markEnd);
                            AppendMarks(sb, src, markEnd + 1, alefMarksEnd);
                        }
                        i = alefMarksEnd - 1;
                        continue;
                    }
                }

                char next = NextBase(src, markEnd - 1);
                bool joinNext = next != '\0' && JoinsBoth(c) && JoinsPrevious(next);

                var forms = s_Forms[c];
                char glyph;
                if (joinPrev && joinNext) glyph = forms[3] != '\0' ? forms[3] : forms[1];
                else if (joinPrev) glyph = forms[1] != '\0' ? forms[1] : forms[0];
                else if (joinNext) glyph = forms[2] != '\0' ? forms[2] : forms[0];
                else glyph = forms[0];

                sb.Append(glyph);
                if (keepMarks) AppendMarks(sb, src, markStart, markEnd);
                i = markEnd - 1;
            }
            return sb.ToString();
        }

        static void AppendMarks(StringBuilder sb, string s, int from, int to)
        {
            for (int k = from; k < to; k++) sb.Append(s[k]);
        }

        static char PrevBase(string s, int i)
        {
            for (int k = i - 1; k >= 0; k--)
                if (!IsTransparent(s[k]))
                    return s_Forms.ContainsKey(s[k]) || s[k] == Tatweel ? s[k] : '\0';
            return '\0';
        }

        static char NextBase(string s, int i)
        {
            for (int k = i + 1; k < s.Length; k++)
                if (!IsTransparent(s[k]))
                    return s_Forms.ContainsKey(s[k]) || s[k] == Tatweel ? s[k] : '\0';
            return '\0';
        }

        static bool IsStrongLtr(char c) =>
            (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
            (c >= 'À' && c <= 'ɏ') || c == 'ʿ' || c == 'ʾ';

        static bool IsLtrJoiner(char c) => c == ' ' || c == '.' || c == ',' || c == ':' || c == '-' || c == '/' || c == '\'' || c == '%';

        /// <summary>
        /// Reverses runs of left-to-right characters (and the neutral characters between them) so TMP's
        /// RTL layout displays them in reading order. Tags &lt;...&gt; are skipped. Brackets are mirrored.
        /// </summary>
        public static string ReverseLtrRuns(string s)
        {
            var chars = s.ToCharArray();
            int i = 0;
            while (i < chars.Length)
            {
                if (chars[i] == '<')
                {
                    int close = System.Array.IndexOf(chars, '>', i);
                    if (close > i) { i = close + 1; continue; }
                }
                if (!IsStrongLtr(chars[i]))
                {
                    if (chars[i] == '(') chars[i] = ')';
                    else if (chars[i] == ')') chars[i] = '(';
                    i++;
                    continue;
                }
                int start = i;
                int lastStrong = i;
                int j = i + 1;
                while (j < chars.Length && chars[j] != '<' && (IsStrongLtr(chars[j]) || IsLtrJoiner(chars[j])))
                {
                    if (IsStrongLtr(chars[j])) lastStrong = j;
                    j++;
                }
                System.Array.Reverse(chars, start, lastStrong - start + 1);
                i = lastStrong + 1;
            }
            return new string(chars);
        }
    }
}
