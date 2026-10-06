using System;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// A merchant's brass tray. A coin resting on (or dropped into) the tray area — not still held — is
    /// taken as payment once. Uses a trigger plus a per-frame bounds check, so a coin that settles and
    /// falls asleep inside the area still counts.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PayTray : MonoBehaviour
    {
        public event Action<HegraItem> CoinPlaced;
        public bool accepting;
        public HegraItem watchedCoin;

        BoxCollider m_Zone;

        void Awake()
        {
            m_Zone = GetComponent<BoxCollider>();
            m_Zone.isTrigger = true;
        }

        void OnTriggerEnter(Collider other) => Consider(other.GetComponentInParent<HegraItem>());
        void OnTriggerStay(Collider other) => Consider(other.GetComponentInParent<HegraItem>());

        void Update()
        {
            if (accepting && watchedCoin && watchedCoin.gameObject.activeInHierarchy && Inside(watchedCoin.transform.position))
                Consider(watchedCoin);
        }

        bool Inside(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world) - m_Zone.center;
            var half = m_Zone.size * 0.5f + Vector3.one * 0.02f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        void Consider(HegraItem item)
        {
            if (!accepting || !item || item.kind != ItemKind.Coin || item.IsHeldByHand) return;
            accepting = false; // exactly one payment per request
            CoinPlaced?.Invoke(item);
        }
    }
}
