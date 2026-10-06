using TMPro;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>Binds a TMP text to a localization key and refreshes it when the language changes.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] string m_Key;
        [SerializeField] TextAlignmentOptions m_Alignment = TextAlignmentOptions.Center;

        TMP_Text m_Text;
        object[] m_Args;

        public string Key
        {
            get => m_Key;
            set { m_Key = value; m_Args = null; Refresh(); }
        }

        public void SetKey(string key, TextAlignmentOptions alignment)
        {
            m_Alignment = alignment;
            Key = key;
        }

        public void SetFormatted(string key, params object[] args)
        {
            m_Key = key;
            m_Args = args;
            Refresh();
        }

        void Awake() => m_Text = GetComponent<TMP_Text>();

        void OnEnable()
        {
            Loc.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.LanguageChanged -= Refresh;

        public void Refresh()
        {
            if (m_Text == null) m_Text = GetComponent<TMP_Text>();
            if (string.IsNullOrEmpty(m_Key)) return;
            var raw = m_Args != null ? Loc.Format(m_Key, m_Args) : Loc.Get(m_Key);
            Loc.Apply(m_Text, raw, m_Alignment);
        }
    }
}
