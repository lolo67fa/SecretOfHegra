using TMPro;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// The player's personalized clay shard: decorative Nabataean-inspired letters (clearly labelled as an
    /// approximation) above the readable modern name. Carries the stable shard id used by the final door.
    /// </summary>
    [RequireComponent(typeof(HegraItem))]
    public class NameShard : MonoBehaviour
    {
        public static NameShard Instance { get; private set; }

        public TextMeshPro nabataeanText;
        public TextMeshPro modernText;
        public TextMeshPro noteText;

        public string ShardId { get; private set; } = "";
        public string PlayerName { get; private set; } = "";
        public HegraItem Item => GetComponent<HegraItem>();

        int m_GlyphCount;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable() => Loc.LanguageChanged += RefreshNote;
        void OnDisable() => Loc.LanguageChanged -= RefreshNote;

        /// <summary>Writes the name onto the shard. Engrave progress 0 hides the letters until carved.</summary>
        public void Inscribe(string playerName, string shardId, float engraveProgress)
        {
            PlayerName = playerName ?? "";
            ShardId = shardId ?? "";
            var gm = GameManager.Instance;
            var result = gm != null ? gm.Nabataean.Convert(PlayerName) : default;
            m_GlyphCount = result.glyphCount;
            if (nabataeanText)
            {
                if (gm && gm.NabataeanFont) nabataeanText.font = gm.NabataeanFont;
                nabataeanText.isRightToLeftText = true;
                nabataeanText.alignment = TextAlignmentOptions.Center;
                nabataeanText.text = result.displayText ?? "";
            }
            if (modernText) Loc.Apply(modernText, PlayerName, TextAlignmentOptions.Center);
            RefreshNote();
            SetEngraveProgress(engraveProgress);
        }

        public void SetEngraveProgress(float t)
        {
            t = Mathf.Clamp01(t);
            if (nabataeanText)
            {
                // RTL: reveal from the right, i.e. in logical order.
                nabataeanText.maxVisibleCharacters = Mathf.CeilToInt(t * Mathf.Max(1, nabataeanText.textInfo.characterCount > 0 ? nabataeanText.textInfo.characterCount : m_GlyphCount + 2));
            }
            if (modernText) modernText.alpha = t >= 0.999f ? 1f : 0.25f;
        }

        void RefreshNote()
        {
            if (noteText) Loc.ApplyKey(noteText, "shard.note", TextAlignmentOptions.Center);
        }
    }
}
