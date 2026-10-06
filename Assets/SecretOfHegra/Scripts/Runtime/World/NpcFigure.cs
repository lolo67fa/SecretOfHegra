using UnityEngine;

namespace SecretOfHegra
{
    public enum NpcGesture { Idle, Talk, Wave, Point, Strum, Carve, Clap, Drum }

    /// <summary>
    /// Lightweight procedural animation for placeholder characters built from simple meshes: breathing,
    /// turning the body (yaw only) toward the player, and arm gestures. Replace with an Animator later.
    /// </summary>
    public class NpcFigure : MonoBehaviour
    {
        public string speakerId;
        public Transform body;
        public Transform head;
        public Transform leftArm;
        public Transform rightArm;
        public bool facePlayer = true;
        public float faceDistance = 7f;
        public NpcGesture idleGesture = NpcGesture.Idle;

        NpcGesture m_Gesture;
        float m_GestureUntil;
        Transform m_PointTarget;
        float m_Seed;
        Quaternion m_LeftRest, m_RightRest;
        Vector3 m_BodyScale;
        float m_Yaw;

        void Awake()
        {
            m_Seed = Random.value * 6f;
            if (leftArm) m_LeftRest = leftArm.localRotation;
            if (rightArm) m_RightRest = rightArm.localRotation;
            if (body) m_BodyScale = body.localScale;
            m_Yaw = transform.eulerAngles.y;
            m_Gesture = idleGesture;
        }

        void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm && gm.Dialogue) gm.Dialogue.LineStarted += OnLineStarted;
        }

        void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm && gm.Dialogue) gm.Dialogue.LineStarted -= OnLineStarted;
        }

        void OnLineStarted(string speaker)
        {
            if (!string.IsNullOrEmpty(speakerId) && speaker == speakerId)
                Play(NpcGesture.Talk, 3.5f);
        }

        public void Play(NpcGesture g, float seconds, Transform pointTarget = null)
        {
            m_Gesture = g;
            m_GestureUntil = Time.time + seconds;
            m_PointTarget = pointTarget;
        }

        void Update()
        {
            float t = Time.time + m_Seed;
            if (Time.time > m_GestureUntil) m_Gesture = idleGesture;

            if (body)
                body.localScale = new Vector3(m_BodyScale.x, m_BodyScale.y * (1f + Mathf.Sin(t * 1.6f) * 0.012f), m_BodyScale.z);

            var gm = GameManager.Instance;
            if (facePlayer && gm && gm.Rig)
            {
                var to = gm.Rig.Head.position - transform.position;
                to.y = 0f;
                if (to.magnitude < faceDistance && to.sqrMagnitude > 0.01f)
                {
                    float goal = Quaternion.LookRotation(to).eulerAngles.y;
                    m_Yaw = Mathf.MoveTowardsAngle(m_Yaw, goal, 90f * Time.deltaTime);
                    transform.rotation = Quaternion.Euler(0f, m_Yaw, 0f);
                }
            }

            if (head)
                head.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 3f, Mathf.Sin(t * 0.43f) * 6f, 0f);

            Quaternion l = m_LeftRest, r = m_RightRest;
            switch (m_Gesture)
            {
                case NpcGesture.Talk:
                    r = m_RightRest * Quaternion.Euler(-35f + Mathf.Sin(t * 4f) * 12f, 0f, 10f);
                    l = m_LeftRest * Quaternion.Euler(-15f + Mathf.Sin(t * 3.1f) * 6f, 0f, -5f);
                    break;
                case NpcGesture.Wave:
                    r = m_RightRest * Quaternion.Euler(-150f, 0f, 20f + Mathf.Sin(t * 8f) * 25f);
                    break;
                case NpcGesture.Point:
                    r = m_RightRest * Quaternion.Euler(-80f, 0f, 0f);
                    if (m_PointTarget && rightArm)
                    {
                        var dir = (m_PointTarget.position - rightArm.position).normalized;
                        rightArm.rotation = Quaternion.FromToRotation(-Vector3.up, dir);
                        r = rightArm.localRotation;
                    }
                    break;
                case NpcGesture.Strum:
                    l = m_LeftRest * Quaternion.Euler(-60f, 20f, 0f);
                    r = m_RightRest * Quaternion.Euler(-55f + Mathf.Sin(t * 10f) * 10f, -20f, 0f);
                    break;
                case NpcGesture.Carve:
                    r = m_RightRest * Quaternion.Euler(-70f + Mathf.Abs(Mathf.Sin(t * 6f)) * -25f, 0f, 0f);
                    l = m_LeftRest * Quaternion.Euler(-60f, 0f, 0f);
                    break;
                case NpcGesture.Clap:
                    float c = Mathf.Abs(Mathf.Sin(t * 5f));
                    l = m_LeftRest * Quaternion.Euler(-70f, 0f, -25f * c);
                    r = m_RightRest * Quaternion.Euler(-70f, 0f, 25f * c);
                    break;
                case NpcGesture.Drum:
                    l = m_LeftRest * Quaternion.Euler(-50f + Mathf.Abs(Mathf.Sin(t * 7f)) * -15f, 0f, 0f);
                    r = m_RightRest * Quaternion.Euler(-50f + Mathf.Abs(Mathf.Cos(t * 7f)) * -15f, 0f, 0f);
                    break;
                default:
                    l = m_LeftRest * Quaternion.Euler(Mathf.Sin(t * 0.9f) * 3f, 0f, 0f);
                    r = m_RightRest * Quaternion.Euler(Mathf.Sin(t * 0.8f + 1f) * 3f, 0f, 0f);
                    break;
            }
            float k = 1f - Mathf.Exp(-8f * Time.deltaTime);
            if (leftArm) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, l, k);
            if (rightArm && m_Gesture != NpcGesture.Point) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, r, k);
            else if (rightArm && !m_PointTarget) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, r, k);
        }
    }
}
