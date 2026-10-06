using System.Collections;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// The kohl's gameplay effect: a gradual, subtle readability boost (slightly brighter ambient light,
    /// stronger lamp reveal and carving contrast) applied over several seconds. It never darkens anything
    /// and never flashes; visibility before applying kohl is already fully playable.
    /// </summary>
    public class KohlEffect : MonoBehaviour
    {
        public static KohlEffect Instance { get; private set; }

        [SerializeField] float m_Duration = 5f;
        [SerializeField] float m_AmbientBoost = 0.18f;

        public static float Amount { get; private set; }

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public static void SetApplied(bool applied, bool instant)
        {
            if (!applied) { Amount = 0f; Push(); return; }
            if (instant || Instance == null) { Amount = 1f; Push(); return; }
            Instance.StopAllCoroutines();
            Instance.StartCoroutine(Instance.Ramp());
        }

        IEnumerator Ramp()
        {
            float start = Amount;
            float t = 0f;
            while (t < m_Duration)
            {
                t += Time.deltaTime;
                Amount = Mathf.Lerp(start, 1f, Mathf.SmoothStep(0f, 1f, t / m_Duration));
                Push();
                yield return null;
            }
            Amount = 1f;
            Push();
        }

        static void Push()
        {
            ClayLamp.RevealBoost = Amount;
            float boost = Instance ? Instance.m_AmbientBoost : 0.18f;
            RenderSettings.ambientIntensity = 1f + boost * Amount;
        }
    }
}
