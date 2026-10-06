using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Kamkam travels ahead of the player: at each stop she waits at that stop's guide point, and she can
    /// appear beside the player to offer the assisted route. NPC relocation is a quick shrink/grow, never
    /// a camera effect.
    /// </summary>
    public class KamkamGuide : MonoBehaviour
    {
        public static KamkamGuide Instance { get; private set; }

        public NpcFigure figure;
        Vector3 m_BaseScale;
        Vector3 m_TargetPos;
        Quaternion m_TargetRot;
        float m_Scale = 1f;
        bool m_Moving;

        void Awake()
        {
            Instance = this;
            m_BaseScale = transform.localScale;
            m_TargetPos = transform.position;
            m_TargetRot = transform.rotation;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm) gm.StepEntered += OnStepEntered;
        }

        void OnStepEntered(QuestStep step)
        {
            var gm = GameManager.Instance;
            var stop = gm.GetStop(QuestFlow.StopOf(step));
            if (stop && stop.guidePoint) MoveTo(stop.guidePoint.position, stop.guidePoint.rotation);
        }

        public void MoveTo(Vector3 pos, Quaternion rot)
        {
            if ((pos - transform.position).sqrMagnitude < 0.01f) return;
            m_TargetPos = pos;
            m_TargetRot = rot;
            m_Moving = true;
        }

        /// <summary>Places Kamkam about 1.6 m in front of the player, slightly to the side, facing them.</summary>
        public void AppearNearPlayer()
        {
            var gm = GameManager.Instance;
            if (!gm || !gm.Rig) return;
            var fwd = gm.Rig.FlatForward;
            var right = Vector3.Cross(Vector3.up, fwd);
            var pos = gm.Rig.Head.position + fwd * 1.6f + right * 0.5f;
            pos.y = gm.Rig.Origin ? gm.Rig.Origin.Origin.transform.position.y : 0f;
            if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                pos.y = hit.point.y;
            MoveTo(pos, Quaternion.LookRotation(-fwd));
        }

        void Update()
        {
            if (!m_Moving) return;
            // shrink, jump, grow (0.6 s total)
            if ((transform.position - m_TargetPos).sqrMagnitude > 0.0001f)
            {
                m_Scale = Mathf.MoveTowards(m_Scale, 0f, Time.deltaTime * 3.5f);
                if (m_Scale <= 0f) transform.SetPositionAndRotation(m_TargetPos, m_TargetRot);
            }
            else
            {
                m_Scale = Mathf.MoveTowards(m_Scale, 1f, Time.deltaTime * 3.5f);
                if (m_Scale >= 1f) m_Moving = false;
            }
            transform.localScale = m_BaseScale * Mathf.Max(0.001f, m_Scale);
        }
    }
}
