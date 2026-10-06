using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Waist anchor that follows the head position and (lazily) its yaw, carrying the belt hook for the
    /// lamp and the satchel for the name shard within comfortable seated or standing reach.
    /// </summary>
    public class BodyAnchor : MonoBehaviour
    {
        public Transform head;
        public float waistDrop = 0.62f;
        public float minWaistHeightAboveFloor = 0.55f;
        public Transform floorReference;
        public Transform beltHook;
        public Transform satchel;
        public float sideOffset = 0.2f;
        public float forwardOffset = 0.16f;

        float m_Yaw;
        Handedness m_Hand = Handedness.Right;

        public void SetHandedness(Handedness hand)
        {
            m_Hand = hand;
            Layout();
        }

        void Start()
        {
            if (head) m_Yaw = head.eulerAngles.y;
            Layout();
        }

        void Layout()
        {
            // Lamp hook on the off-hand side so the dominant hand stays free for the chisel and UI.
            float lampSide = m_Hand == Handedness.Right ? -1f : 1f;
            if (beltHook) beltHook.localPosition = new Vector3(lampSide * sideOffset, 0f, forwardOffset);
            if (satchel) satchel.localPosition = new Vector3(-lampSide * sideOffset, 0.02f, forwardOffset);
        }

        void LateUpdate()
        {
            if (!head) return;
            float headYaw = head.eulerAngles.y;
            if (Mathf.Abs(Mathf.DeltaAngle(headYaw, m_Yaw)) > 35f)
                m_Yaw = Mathf.MoveTowardsAngle(m_Yaw, headYaw, 240f * Time.deltaTime);
            var pos = head.position - Vector3.up * waistDrop;
            if (floorReference)
                pos.y = Mathf.Max(pos.y, floorReference.position.y + minWaistHeightAboveFloor);
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, m_Yaw, 0f));
        }
    }
}
