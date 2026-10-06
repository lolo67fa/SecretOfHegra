using System;
using TMPro;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// A faint carving that only appears in the clay lamp's light: visibility rises as the lamp's flame comes
    /// within range with a clear line of sight. Holding it revealed for a moment counts as "found".
    /// Implemented with per-object alpha (no custom stereo shader needed), so it is cheap and VR-safe.
    /// </summary>
    public class LampRevealTarget : MonoBehaviour
    {
        public TMP_Text[] carvings;
        public Renderer glow;
        public Collider wall;
        /// <summary>Colliders under this transform (the carved rock itself) never block the lamp's light.</summary>
        public Transform ignoreUnder;
        public float range = 1.3f;
        public float dwellSeconds = 1.2f;
        public bool active = true;
        [Range(0f, 1f)] public float hintAlpha = 0.04f;

        public event Action Revealed;
        public bool IsRevealed { get; private set; }
        public float Visibility { get; private set; }

        float m_Dwell;
        MaterialPropertyBlock m_Mpb;
        static readonly int k_Intensity = Shader.PropertyToID("_Intensity");

        void Awake() => Apply(0f);

        public void SetRevealed(bool revealed)
        {
            IsRevealed = revealed;
            Apply(revealed ? 1f : 0f);
        }

        void Update()
        {
            if (IsRevealed) return;
            float target = 0f;
            var lamp = ClayLamp.Instance;
            if (active && lamp)
            {
                var from = lamp.RevealPosition;
                var to = transform.position;
                float r = range * (1f + KohlEffect.Amount * 0.3f);
                float d = Vector3.Distance(from, to);
                if (d < r && ClearLine(from, to))
                    target = Mathf.Clamp01(1f - (d - r * 0.35f) / (r * 0.65f));
            }
            Visibility = Mathf.MoveTowards(Visibility, target, Time.deltaTime * 2.5f);
            Apply(Visibility);

            m_Dwell = Visibility > 0.75f ? m_Dwell + Time.deltaTime : 0f;
            if (m_Dwell >= dwellSeconds)
            {
                IsRevealed = true;
                Apply(1f);
                Revealed?.Invoke();
            }
        }

        bool ClearLine(Vector3 from, Vector3 to)
        {
            // something other than the carved wall (or the lamp/player) between flame and carving blocks the light
            var dir = to - from;
            var hits = Physics.RaycastAll(from, dir.normalized, dir.magnitude * 0.97f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (wall && h.collider == wall) continue;
                if (ignoreUnder && h.collider.transform.IsChildOf(ignoreUnder)) continue;
                if (h.collider.GetComponentInParent<HegraItem>()) continue;
                if (h.collider.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()) continue;
                return false;
            }
            return true;
        }

        void Apply(float v)
        {
            float a = Mathf.Lerp(hintAlpha, 1f, v);
            if (carvings != null)
                foreach (var t in carvings) if (t) t.alpha = a;
            if (glow)
            {
                m_Mpb ??= new MaterialPropertyBlock();
                glow.GetPropertyBlock(m_Mpb);
                m_Mpb.SetFloat(k_Intensity, v * 0.45f);
                glow.SetPropertyBlock(m_Mpb);
                glow.enabled = v > 0.01f;
            }
        }
    }
}
