using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Points out Shuʿayd's coin plate once a journey has started: a small bobbing golden arrow above it. Hidden in
    /// the main menu so nothing covers the menu; shown from the first camp step until the player has taken the
    /// coins (GameState.coinsReceived), then gone for good.
    /// </summary>
    public class CampBeacon : MonoBehaviour
    {
        public Transform arrow;
        public float bobHeight = 0.06f;

        Vector3 m_ArrowBase;

        void Awake()
        {
            if (arrow) m_ArrowBase = arrow.localPosition;
        }

        void LateUpdate()
        {
            var gm = GameManager.Instance;
            bool show = gm && gm.InGame && gm.State != null && !gm.State.coinsReceived
                        && gm.State.step <= QuestStep.CampTakeCoins;
            if (arrow && arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
            if (!show || !arrow) return;

            arrow.localPosition = m_ArrowBase + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight);
            var cam = Camera.main;
            if (!cam) return;
            var d = arrow.position - cam.transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) arrow.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }
}
