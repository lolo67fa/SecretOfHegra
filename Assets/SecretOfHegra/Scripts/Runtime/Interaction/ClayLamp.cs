using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// The clay oil lamp: the single realtime light in the game (warm, short range), a flickering flame
    /// that subtly leans toward the current objective as a hint, and the source position for
    /// lamp-revealed carvings (published as a global shader vector).
    /// </summary>
    public class ClayLamp : MonoBehaviour
    {
        [SerializeField] Light m_Light;
        [SerializeField] Transform m_Flame;
        [SerializeField] Transform m_RevealOrigin;
        [SerializeField] float m_RevealRadius = 1.6f;
        [SerializeField] float m_BaseIntensity = 1.1f;
        [SerializeField] float m_LeanDegrees = 22f;

        public static ClayLamp Instance { get; private set; }
        public static readonly int LampPosId = Shader.PropertyToID("_HegraLampPos");
        public static readonly int RevealBoostId = Shader.PropertyToID("_HegraRevealBoost");

        public Vector3 RevealPosition => m_RevealOrigin ? m_RevealOrigin.position : transform.position;
        public float RevealRadius => m_RevealRadius * (1f + RevealBoost * 0.35f);

        /// <summary>0..1, raised gradually by the kohl to make dark carvings easier to read.</summary>
        public static float RevealBoost { get; set; }

        public Light Light { get => m_Light; set => m_Light = value; }
        public Transform Flame { get => m_Flame; set => m_Flame = value; }
        public Transform RevealOrigin { get => m_RevealOrigin; set => m_RevealOrigin = value; }

        float m_Seed;
        Vector3 m_FlameBaseScale = Vector3.one;
        Quaternion m_Lean = Quaternion.identity;

        void Awake()
        {
            Instance = this;
            m_Seed = Random.value * 10f;
            if (m_Flame) m_FlameBaseScale = m_Flame.localScale;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            float t = Time.time * 9f + m_Seed;
            float flicker = 0.88f + 0.08f * Mathf.PerlinNoise(t, 0.3f) + 0.04f * Mathf.Sin(t * 2.3f);
            if (m_Light) m_Light.intensity = m_BaseIntensity * flicker;

            if (m_Flame)
            {
                m_Flame.localScale = new Vector3(m_FlameBaseScale.x, m_FlameBaseScale.y * (0.9f + 0.2f * flicker), m_FlameBaseScale.z);

                // Lean toward the objective: rotate "up" a little toward the flat direction to the target.
                Quaternion goal = Quaternion.identity;
                var target = ObjectiveBeacon.Current;
                if (target)
                {
                    var dir = target.position - m_Flame.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.25f)
                    {
                        var axis = Vector3.Cross(Vector3.up, dir.normalized);
                        goal = Quaternion.AngleAxis(m_LeanDegrees, axis);
                    }
                }
                m_Lean = Quaternion.Slerp(m_Lean, goal, 1f - Mathf.Exp(-3f * Time.deltaTime));
                // keep the flame upright in world space, then apply the lean
                m_Flame.rotation = m_Lean;
            }

            var p = RevealPosition;
            Shader.SetGlobalVector(LampPosId, new Vector4(p.x, p.y, p.z, RevealRadius));
            Shader.SetGlobalFloat(RevealBoostId, RevealBoost);
        }
    }

    /// <summary>The single current objective target, used by the flame hint and the objective arrow.</summary>
    public static class ObjectiveBeacon
    {
        public static Transform Current;
    }
}
