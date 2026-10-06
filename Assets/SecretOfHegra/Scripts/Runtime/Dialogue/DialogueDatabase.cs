using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>Dialogue sequences, speakers and line timing loaded from Data/dialogue.json.</summary>
    public class DialogueDatabase
    {
        [Serializable] public class Line { public string key; public string speaker; public float duration; }
        [Serializable] public class Sequence { public string id; public Line[] lines; }
        [Serializable] public class Speaker { public string id; public string nameKey; public string color; }
        [Serializable] class FileDef { public Speaker[] speakers; public Sequence[] sequences; }

        readonly Dictionary<string, Sequence> m_Sequences = new Dictionary<string, Sequence>();
        readonly Dictionary<string, Speaker> m_Speakers = new Dictionary<string, Speaker>();

        public IEnumerable<Sequence> Sequences => m_Sequences.Values;

        public static DialogueDatabase FromJson(string json)
        {
            var db = new DialogueDatabase();
            if (string.IsNullOrEmpty(json)) return db;
            var def = JsonUtility.FromJson<FileDef>(json);
            if (def?.speakers != null)
                foreach (var s in def.speakers) db.m_Speakers[s.id] = s;
            if (def?.sequences != null)
                foreach (var s in def.sequences) db.m_Sequences[s.id] = s;
            Loc.LoadTable(json); // the same file carries the localized line text in "entries"
            return db;
        }

        public Sequence Get(string id) => id != null && m_Sequences.TryGetValue(id, out var s) ? s : null;
        public bool Has(string id) => id != null && m_Sequences.ContainsKey(id);

        public Speaker GetSpeaker(string id) => id != null && m_Speakers.TryGetValue(id, out var s) ? s : null;

        /// <summary>Subtitle display time when no voice clip exists: reading speed with a comfortable minimum.</summary>
        public static float EstimateDuration(string text, float explicitDuration)
        {
            if (explicitDuration > 0f) return explicitDuration;
            int len = string.IsNullOrEmpty(text) ? 0 : text.Length;
            return Mathf.Clamp(1.4f + len / 14f, 2.8f, 12f);
        }
    }
}
