using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Shows the passing of time without a countdown: the moon (directional light + emissive disc)
    /// sinks from high in the sky toward the western mountains, and the sky/ambient colors cool slightly.
    /// </summary>
    public class MoonController : MonoBehaviour
    {
        public Light moonLight;
        public Transform moonDisc;
        public Material skyMaterial;
        public float startElevation = 48f;
        public float endElevation = 7f;
        public float azimuth = 250f;
        public float azimuthDrift = 25f;
        public float discDistance = 380f;
        public Color startAmbient = new Color(0.22f, 0.25f, 0.40f);
        public Color endAmbient = new Color(0.17f, 0.19f, 0.32f);

        float m_Progress = -1f;
        static readonly int k_Horizon = Shader.PropertyToID("_HorizonColor");

        void Start() => SetProgress(0f);

        public void SetProgress(float t)
        {
            t = Mathf.Clamp01(t);
            if (Mathf.Abs(t - m_Progress) < 0.0005f) return;
            m_Progress = t;
            float elev = Mathf.Lerp(startElevation, endElevation, t);
            float az = azimuth + azimuthDrift * t;
            var rot = Quaternion.Euler(elev, az, 0f);
            if (moonLight)
            {
                moonLight.transform.rotation = rot;
                moonLight.intensity = Mathf.Lerp(0.55f, 0.38f, t);
            }
            if (moonDisc)
            {
                moonDisc.position = -(rot * Vector3.forward) * discDistance;
                moonDisc.rotation = Quaternion.LookRotation(rot * Vector3.forward);
            }
            RenderSettings.ambientLight = Color.Lerp(startAmbient, endAmbient, t);
            if (skyMaterial && skyMaterial.HasProperty(k_Horizon))
                skyMaterial.SetColor(k_Horizon, Color.Lerp(new Color(0.16f, 0.2f, 0.36f), new Color(0.2f, 0.16f, 0.3f), t));
        }
    }
}
