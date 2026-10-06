using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Voice-over clip slots keyed by dialogue line key, one slot per language. Empty slots are allowed:
    /// the line is then shown as a timed subtitle only. The builder creates one slot for every line.
    /// </summary>
    [CreateAssetMenu(menuName = "Secret of Hegra/Voiceover Bank")]
    public class VoiceoverBank : ScriptableObject
    {
        [Serializable]
        public class Slot
        {
            public string key;
            public AudioClip arabic;
            public AudioClip english;
        }

        public List<Slot> slots = new List<Slot>();

        Dictionary<string, Slot> m_Lookup;

        public AudioClip Get(string key, Language language)
        {
            if (m_Lookup == null)
            {
                m_Lookup = new Dictionary<string, Slot>();
                foreach (var s in slots)
                    if (s != null && !string.IsNullOrEmpty(s.key))
                        m_Lookup[s.key] = s;
            }
            if (!m_Lookup.TryGetValue(key, out var slot)) return null;
            return language == Language.Arabic ? (slot.arabic ? slot.arabic : slot.english) : (slot.english ? slot.english : slot.arabic);
        }

        public int CountMissing()
        {
            int n = 0;
            foreach (var s in slots)
                if (s.arabic == null || s.english == null) n++;
            return n;
        }
    }
}
