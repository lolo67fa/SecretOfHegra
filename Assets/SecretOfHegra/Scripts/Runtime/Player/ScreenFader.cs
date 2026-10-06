using System.Collections;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>Comfort fade (to night-blue) used for every relocation and scene transition.</summary>
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] Renderer m_Quad;
        [SerializeField] float m_DefaultDuration = 0.45f;

        MaterialPropertyBlock m_Block;
        float m_Alpha;
        static readonly int k_Color = Shader.PropertyToID("_Color");

        public float Alpha => m_Alpha;
        public Renderer Quad { get => m_Quad; set => m_Quad = value; }

        void Awake()
        {
            m_Block = new MaterialPropertyBlock();
            SetAlpha(1f); // start black; GameManager fades in once the world is ready
        }

        public void SetAlpha(float a)
        {
            m_Alpha = Mathf.Clamp01(a);
            if (!m_Quad) return;
            m_Quad.enabled = m_Alpha > 0.001f;
            m_Block ??= new MaterialPropertyBlock();
            var c = HegraPalette.Night;
            c.a = m_Alpha;
            m_Block.SetColor(k_Color, c);
            m_Quad.SetPropertyBlock(m_Block);
        }

        public IEnumerator FadeTo(float target, float duration = -1f)
        {
            if (duration < 0f) duration = m_DefaultDuration;
            float start = m_Alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return null;
            }
            SetAlpha(target);
        }
    }
}
