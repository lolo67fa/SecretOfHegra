using System;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Kohl jar: holding it in a comfortable zone in front of the face (no contact with the headset or
    /// eyes) for a short moment applies the kohl. A small bar above the jar shows progress.
    /// </summary>
    [RequireComponent(typeof(HegraItem))]
    public class KohlApplicator : MonoBehaviour
    {
        public float holdSeconds = 1.5f;
        public float zoneDistance = 0.32f;
        public Transform progressBar;
        public bool armed;

        public event Action Applied;

        float m_Progress;
        HegraItem m_Item;
        Vector3 m_BarScale;

        void Awake()
        {
            m_Item = GetComponent<HegraItem>();
            if (progressBar) { m_BarScale = progressBar.localScale; progressBar.gameObject.SetActive(false); }
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (!armed || !gm || !gm.Rig) { SetBar(0f, false); return; }
            var head = gm.Rig.Head;
            var to = transform.position - head.position;
            // in front of the face, within reach, roughly at eye/cheek height
            bool inZone = m_Item.IsHeldByHand && to.magnitude < zoneDistance && Vector3.Dot(to.normalized, head.forward) > 0.35f;
            m_Progress = inZone ? m_Progress + Time.deltaTime : Mathf.Max(0f, m_Progress - Time.deltaTime * 2f);
            SetBar(m_Progress / holdSeconds, m_Item.IsHeldByHand);
            if (m_Progress >= holdSeconds)
            {
                m_Progress = 0f;
                armed = false;
                SetBar(0f, false);
                Applied?.Invoke();
            }
        }

        void SetBar(float t, bool visible)
        {
            if (!progressBar) return;
            progressBar.gameObject.SetActive(visible && t > 0.01f);
            progressBar.localScale = new Vector3(m_BarScale.x * Mathf.Clamp01(t), m_BarScale.y, m_BarScale.z);
        }
    }
}
