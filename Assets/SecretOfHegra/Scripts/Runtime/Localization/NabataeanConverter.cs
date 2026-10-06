using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Converts a modern Arabic or English name into a Nabataean-inspired letter sequence using the
    /// editable mapping in Data/nabataean_map.json. The result is a decorative approximation, never a
    /// verified historical transcription. Characters without a mapping, or whose glyph is missing from
    /// the font, become a neutral carved dot instead of a broken glyph.
    /// </summary>
    public class NabataeanConverter
    {
        [Serializable] class LetterDef { public string name; public string cp; public string final; }
        [Serializable] class MapDef { public string src; public string letter; public string pos; }
        [Serializable] class FileDef { public string note_en; public string note_ar; public LetterDef[] letters; public MapDef[] map; }

        public const string MissingGlyph = "·";

        /// <summary>
        /// TextMesh Pro's dynamic atlas cannot add supplementary-plane glyphs here, so the bundled font
        /// (HegraNabataeanPUA) also maps U+10880–U+108AF to U+E880–U+E8AF. Display uses those code points.
        /// </summary>
        public static int ToDisplayCodepoint(int cp) => cp >= 0x10880 && cp <= 0x108AF ? 0xE880 + (cp - 0x10880) : cp;

        public static string ToDisplay(string realUnicode)
        {
            if (string.IsNullOrEmpty(realUnicode)) return realUnicode ?? "";
            var sb = new StringBuilder(realUnicode.Length);
            for (int i = 0; i < realUnicode.Length; i++)
            {
                int cp = char.ConvertToUtf32(realUnicode, i);
                if (char.IsHighSurrogate(realUnicode[i])) i++;
                sb.Append(char.ConvertFromUtf32(ToDisplayCodepoint(cp)));
            }
            return sb.ToString();
        }

        readonly Dictionary<string, (int cp, int final)> m_Letters = new Dictionary<string, (int, int)>();
        readonly List<MapDef> m_Map = new List<MapDef>();
        int m_MaxSrcLength = 1;

        public string NoteEnglish { get; private set; } = "";
        public string NoteArabic { get; private set; } = "";

        /// <summary>Optional glyph check (e.g. TMP font asset lookup). Null means "assume available".</summary>
        public Func<int, bool> hasGlyph;

        public struct Result
        {
            public string text;          // logical (right-to-left) order, real Unicode (UTF-16 surrogate pairs)
            public string displayText;   // same letters mapped to the PUA code points of the bundled game font
            public int glyphCount;
            public int missingCount;
            public List<string> letterNames;
        }

        public static NabataeanConverter FromJson(string json)
        {
            var conv = new NabataeanConverter();
            var def = JsonUtility.FromJson<FileDef>(json);
            if (def == null)
                return conv;
            conv.NoteEnglish = def.note_en ?? "";
            conv.NoteArabic = def.note_ar ?? "";
            if (def.letters != null)
                foreach (var l in def.letters)
                    conv.m_Letters[l.name] = (ParseHex(l.cp), ParseHex(l.final));
            if (def.map != null)
                foreach (var m in def.map)
                {
                    if (string.IsNullOrEmpty(m.src)) continue;
                    m.src = m.src.ToLowerInvariant();
                    conv.m_Map.Add(m);
                    conv.m_MaxSrcLength = Math.Max(conv.m_MaxSrcLength, m.src.Length);
                }
            return conv;
        }

        static int ParseHex(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            return int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var v) ? v : 0;
        }

        public Result Convert(string name)
        {
            var result = new Result { text = "", letterNames = new List<string>() };
            if (string.IsNullOrWhiteSpace(name))
                return result;

            var codepoints = new List<int>();
            var names = new List<string>();
            var words = name.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int w = 0; w < words.Length; w++)
            {
                if (w > 0) { codepoints.Add(' '); names.Add(" "); }
                ConvertWord(StripMarks(words[w]), codepoints, names);
            }

            // collapse doubled letters produced by doubled source letters (e.g. "ll")
            var sb = new StringBuilder();
            int prev = -1;
            for (int i = 0; i < codepoints.Count; i++)
            {
                int cp = codepoints[i];
                if (cp == prev && cp != ' ' && cp != MissingGlyph[0]) continue;
                prev = cp;
                if (cp != ' ' && cp != MissingGlyph[0] && hasGlyph != null && !hasGlyph(ToDisplayCodepoint(cp)))
                {
                    sb.Append(MissingGlyph);
                    result.missingCount++;
                    result.letterNames.Add("?");
                    result.glyphCount++;
                    continue;
                }
                sb.Append(char.ConvertFromUtf32(cp));
                result.letterNames.Add(names[i]);
                if (cp != ' ') result.glyphCount++;
                if (cp == MissingGlyph[0]) result.missingCount++;
            }
            result.text = sb.ToString();
            result.displayText = ToDisplay(result.text);
            return result;
        }

        static string StripMarks(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
                if (!ArabicShaper.IsTransparent(c) && c != 'ـ')
                    sb.Append(c);
            return sb.ToString();
        }

        void ConvertWord(string word, List<int> output, List<string> names)
        {
            int startIndex = output.Count;
            int i = 0;
            while (i < word.Length)
            {
                bool matched = false;
                for (int len = Math.Min(m_MaxSrcLength, word.Length - i); len >= 1 && !matched; len--)
                {
                    var piece = word.Substring(i, len);
                    string pos = i == 0 ? "initial" : (i + len >= word.Length ? "final" : "medial");
                    var def = Find(piece, pos);
                    if (def == null) continue;
                    matched = true;
                    i += len;
                    if (string.IsNullOrEmpty(def.letter)) break; // intentionally silent (short vowels)
                    if (m_Letters.TryGetValue(def.letter, out var cps))
                    {
                        output.Add(cps.cp);
                        names.Add(def.letter);
                    }
                }
                if (!matched)
                {
                    output.Add(MissingGlyph[0]);
                    names.Add("?");
                    i++;
                }
            }

            // final forms on the last letter of the word
            if (output.Count > startIndex)
            {
                int last = output.Count - 1;
                var letterName = names[last];
                if (m_Letters.TryGetValue(letterName, out var cps) && cps.final != 0)
                    output[last] = cps.final;
            }
        }

        MapDef Find(string piece, string pos)
        {
            MapDef any = null;
            foreach (var m in m_Map)
            {
                if (m.src != piece) continue;
                if (m.pos == pos) return m;
                if (m.pos == "any" || string.IsNullOrEmpty(m.pos)) any ??= m;
            }
            return any;
        }
    }
}
