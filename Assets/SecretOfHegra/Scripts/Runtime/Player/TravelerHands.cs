using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// First-person hands and sleeves (linen sleeve, indigo mantle cuff). The appearance setting switches
    /// between a female variant (bronze bracelet, slimmer hand) and a male variant (leather wrist band).
    /// </summary>
    public class TravelerHands : MonoBehaviour
    {
        public Transform[] hands;
        public GameObject[] femaleOnly;
        public GameObject[] maleOnly;
        [Tooltip("Sleeve parts hidden in the desktop simulator, whose virtual hands sit very close to the eyes.")]
        public GameObject[] sleeves;

        bool m_DesktopSimulator;

        /// <summary>Desktop simulator: its virtual hands sit at the eyes, so sleeves and wrist rings are hidden.</summary>
        public void SetSleevesVisible(bool visible)
        {
            m_DesktopSimulator = !visible;
            foreach (var g in sleeves) if (g) g.SetActive(visible);
            var gm = GameManager.Instance;
            if (gm) Apply(gm.Settings);
        }

        void Start()
        {
            var gm = GameManager.Instance;
            if (!gm) return;
            gm.SettingsChanged += Apply;
            Apply(gm.Settings);
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm) gm.SettingsChanged -= Apply;
        }

        public void Apply(SettingsData s)
        {
            bool female = s.appearance == Appearance.Female;
            foreach (var g in femaleOnly) if (g) g.SetActive(female && !m_DesktopSimulator);
            foreach (var g in maleOnly) if (g) g.SetActive(!female && !m_DesktopSimulator);
            foreach (var h in hands)
                if (h) h.localScale = female ? new Vector3(0.9f, 1f, 0.95f) : Vector3.one;
        }
    }
}
