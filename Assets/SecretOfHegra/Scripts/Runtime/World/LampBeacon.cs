using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Makes the clay lamp impossible to miss before it is first picked up: a softly pulsing golden halo around
    /// it and a bobbing golden arrow above it. Both disappear for good once the player has grabbed the lamp
    /// (GameState.lampAcquired), and stay visible in the main menu so the lamp reads from the first second.
    /// </summary>
    public class LampBeacon : MonoBehaviour
    {
        public Renderer halo;
        public Transform arrow;
        public float pulseSpeed = 2.4f;
        public float bobHeight = 0.06f;

        MaterialPropertyBlock m_Mpb;
        Vector3 m_ArrowBase;
        static readonly int k_Intensity = Shader.PropertyToID("_Intensity");

        void Awake()
        {
            if (arrow) m_ArrowBase = arrow.localPosition;
        }

        void LateUpdate()
        {
            var gm = GameManager.Instance;
            bool show = !(gm && gm.State != null && gm.State.lampAcquired) && !(gm && gm.Lamp && gm.Lamp.IsHeldByHand);
            if (halo) halo.enabled = show;
            if (arrow && arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
            if (!show) return;

            float p = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            if (halo)
            {
                m_Mpb ??= new MaterialPropertyBlock();
                halo.GetPropertyBlock(m_Mpb);
                m_Mpb.SetFloat(k_Intensity, Mathf.Lerp(0.55f, 1.25f, p));
                halo.SetPropertyBlock(m_Mpb);
                FaceCamera(halo.transform, false);
            }
            if (arrow)
            {
                arrow.localPosition = m_ArrowBase + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight);
                FaceCamera(arrow, true);
            }
        }

        static void FaceCamera(Transform t, bool yawOnly)
        {
            var cam = Camera.main;
            if (!cam) return;
            var d = t.position - cam.transform.position;
            if (yawOnly) d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }
}
