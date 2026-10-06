using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Cheap stylized "reflection": a small procedurally painted portrait of the traveler on a bronze disc
    /// (no realtime mirror camera). It redraws when kohl is applied or the appearance setting changes.
    /// </summary>
    public class BronzeMirror : MonoBehaviour
    {
        public Renderer portrait;
        const int Size = 128;

        Texture2D m_Tex;
        bool m_Kohl;
        Appearance m_Look;
        static readonly int k_BaseMap = Shader.PropertyToID("_BaseMap");

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm) gm.SettingsChanged += s => Redraw(m_Kohl, s.appearance);
            Redraw(false, gm ? gm.Settings.appearance : Appearance.Female);
        }

        void OnDestroy()
        {
            if (m_Tex) Destroy(m_Tex);
        }

        public void Redraw(bool kohl, Appearance look)
        {
            m_Kohl = kohl;
            m_Look = look;
            if (!m_Tex)
            {
                m_Tex = new Texture2D(Size, Size, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp, name = "MirrorPortrait" };
            }
            var bronze = new Color(0.62f, 0.44f, 0.24f);
            var skin = new Color(0.66f, 0.47f, 0.35f);
            var cloth = look == Appearance.Female ? HegraPalette.Indigo : HegraPalette.Earth;
            var kohlCol = HegraPalette.Kohl;
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float u = (x - Size * 0.5f) / Size, v = (y - Size * 0.5f) / Size;
                    var c = bronze * (0.85f + 0.15f * Mathf.Sin(u * 9f + v * 4f));
                    // head covering
                    if (Ellipse(u, v + 0.05f, 0.27f, 0.36f)) c = Color.Lerp(c, cloth, 0.85f);
                    // face
                    if (Ellipse(u, v + 0.02f, 0.17f, 0.23f)) c = Color.Lerp(c, skin, 0.9f);
                    // shoulders
                    if (v < -0.28f && Mathf.Abs(u) < 0.45f - (v + 0.5f) * 0.5f) c = Color.Lerp(c, cloth, 0.85f);
                    // eyes (+ kohl outline)
                    foreach (var ex in new[] { -0.07f, 0.07f })
                    {
                        if (kohl && Ellipse(u - ex, v - 0.05f, 0.05f, 0.022f)) c = kohlCol;
                        if (Ellipse(u - ex, v - 0.05f, 0.03f, 0.014f)) c = new Color(0.1f, 0.07f, 0.06f);
                    }
                    // tarnish toward the rim
                    float r = Mathf.Sqrt(u * u + v * v);
                    c *= Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((r - 0.38f) * 6f));
                    px[y * Size + x] = c;
                }
            m_Tex.SetPixels(px);
            m_Tex.Apply(false);
            if (portrait)
            {
                var mpb = new MaterialPropertyBlock();
                portrait.GetPropertyBlock(mpb);
                mpb.SetTexture(k_BaseMap, m_Tex);
                portrait.SetPropertyBlock(mpb);
            }
        }

        static bool Ellipse(float x, float y, float a, float b) => (x * x) / (a * a) + (y * y) / (b * b) <= 1f;
    }
}
