using System.Collections.Generic;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// A ring of small lights that fills while the player holds their gaze on something.
    /// Built from plain primitives (no sprites) and billboarded toward the player's head.
    /// </summary>
    public class GazeRing : MonoBehaviour
    {
        public List<Renderer> pips = new List<Renderer>();
        public Material onMaterial;
        public Material offMaterial;

        float m_Progress = -1f;

        public void SetProgress(float t)
        {
            t = Mathf.Clamp01(t);
            if (Mathf.Approximately(t, m_Progress)) return;
            m_Progress = t;
            int lit = Mathf.RoundToInt(t * pips.Count);
            for (int i = 0; i < pips.Count; i++)
                if (pips[i]) pips[i].sharedMaterial = i < lit ? onMaterial : offMaterial;
        }

        /// <summary>Moves the ring in front of <paramref name="target"/> (slightly toward the head) and shows it.</summary>
        public void ShowAt(Vector3 target, Vector3 head, float scale)
        {
            var toHead = head - target;
            float d = toHead.magnitude;
            transform.position = target + toHead.normalized * Mathf.Min(1.2f, d * 0.08f);
            transform.rotation = Quaternion.LookRotation(-toHead.normalized, Vector3.up);
            transform.localScale = Vector3.one * scale;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            SetProgress(0f);
        }
    }
}
