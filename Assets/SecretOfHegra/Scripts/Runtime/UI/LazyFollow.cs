using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Keeps a world-space panel comfortably in view: it eases toward a point in front of the head
    /// (yaw only, never rolls) and only re-centers once the head has turned past a threshold, so text
    /// does not swim with every small head motion.
    /// </summary>
    public class LazyFollow : MonoBehaviour
    {
        public Transform target;
        public float distance = 1.4f;
        public float heightOffset = -0.25f;
        public float yawThreshold = 30f;
        public float followSpeed = 3f;
        [Tooltip("Placed in front of the head only when SnapNow is called (e.g. once per objective), then stays put in the world.")]
        public bool stayPut;

        Vector3 m_GoalPos;
        Quaternion m_GoalRot;
        bool m_HasGoal;
        float m_AnchorYaw;

        void OnEnable() => SnapNow();

        public void SnapNow()
        {
            if (!target) return;
            m_AnchorYaw = target.eulerAngles.y;
            ComputeGoal();
            transform.SetPositionAndRotation(m_GoalPos, m_GoalRot);
            m_HasGoal = true;
        }

        void LateUpdate()
        {
            if (!target || stayPut) return;
            float yaw = target.eulerAngles.y;
            if (!m_HasGoal || Mathf.Abs(Mathf.DeltaAngle(yaw, m_AnchorYaw)) > yawThreshold)
                m_AnchorYaw = yaw;
            ComputeGoal();
            m_HasGoal = true;
            float k = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, m_GoalPos, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, m_GoalRot, k);
        }

        void ComputeGoal()
        {
            var fwd = Quaternion.Euler(0f, m_AnchorYaw, 0f) * Vector3.forward;
            m_GoalPos = target.position + fwd * distance + Vector3.up * heightOffset;
            m_GoalRot = Quaternion.LookRotation(fwd, Vector3.up);
        }
    }
}
