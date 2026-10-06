using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>Cheap flicker for fire and lantern glow meshes (scale only; no lights, no allocations).</summary>
    public class FlameFlicker : MonoBehaviour
    {
        public float amount = 0.12f;
        public float speed = 7f;
        public bool billboardYaw;

        Vector3 m_Base;
        float m_Seed;
        Transform m_Cam;

        void Awake()
        {
            m_Base = transform.localScale;
            m_Seed = Random.value * 20f;
        }

        void Update()
        {
            float t = Time.time * speed + m_Seed;
            float f = 1f + amount * (Mathf.PerlinNoise(t, m_Seed) - 0.5f) * 2f;
            transform.localScale = new Vector3(m_Base.x * (2f - f) * 0.5f + m_Base.x * 0.5f, m_Base.y * f, m_Base.z);
            if (billboardYaw)
            {
                if (!m_Cam) m_Cam = Camera.main ? Camera.main.transform : null;
                if (m_Cam)
                {
                    var d = m_Cam.position - transform.position;
                    d.y = 0f;
                    if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(-d);
                }
            }
        }
    }
}
