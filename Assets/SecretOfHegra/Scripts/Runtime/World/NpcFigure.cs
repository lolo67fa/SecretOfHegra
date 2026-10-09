using System.Collections.Generic;
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
        [Tooltip("Where speech bubbles attach (top of the head). Falls back to the head when empty.")]
        public Transform dialogueAnchor;
        [Tooltip("Soft glow above the head, shown while this character is speaking.")]
        public Renderer speakGlow;

        static readonly List<NpcFigure> s_All = new List<NpcFigure>();

        /// <summary>The speaking character with this speaker id closest to the player (null if none in the scene).</summary>
        public static NpcFigure FindSpeaker(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var gm = GameManager.Instance;
            var eye = gm && gm.Rig ? gm.Rig.Head.position : Vector3.zero;
            NpcFigure best = null;
            float bestD = float.MaxValue;
            foreach (var n in s_All)
            {
                if (!n || n.speakerId != id) continue;
                float d = (n.transform.position - eye).sqrMagnitude;
                if (d < bestD) { bestD = d; best = n; }
            }
            return best;
        }

        /// <summary>Nearest named (speaking) character within range of a point, e.g. the owner of a tray.</summary>
        public static NpcFigure NearestSpeaker(Vector3 pos, float range)
        {
            NpcFigure best = null;
            float bestD = range * range;
            foreach (var n in s_All)
            {
                if (!n || string.IsNullOrEmpty(n.speakerId)) continue;
                var d = n.transform.position - pos; d.y = 0f;
                if (d.sqrMagnitude < bestD) { bestD = d.sqrMagnitude; best = n; }
            }
            return best;
        }

        public Vector3 DialogueAnchorPosition =>
            dialogueAnchor ? dialogueAnchor.position : head ? head.position + Vector3.up * 0.12f : transform.position + Vector3.up * 1.75f;

        /// <summary>True while a dialogue line of this character is playing.</summary>
        public bool IsSpeaking
        {
            get
            {
                var gm = GameManager.Instance;
                return gm && gm.Dialogue && gm.Dialogue.IsPlaying && !string.IsNullOrEmpty(speakerId) && gm.Dialogue.CurrentSpeaker == speakerId;
            }
        }

        float m_Glow;
        Quaternion m_BodyRot = Quaternion.identity;

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
            if (body) { m_BodyScale = body.localScale; m_BodyRot = body.localRotation; }
            m_Yaw = transform.eulerAngles.y;
            m_Gesture = idleGesture;
        }

        void OnEnable()
        {
            if (!s_All.Contains(this)) s_All.Add(this);
            var gm = GameManager.Instance;
            if (gm && gm.Dialogue) gm.Dialogue.LineStarted += OnLineStarted;
        }

        void OnDisable()
        {
            s_All.Remove(this);
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

            bool speaking = IsSpeaking;
            if (head)
            {
                // a little nod while talking, slow looking around otherwise
                float nod = speaking ? Mathf.Sin(t * 7.5f) * 4f + Mathf.Sin(t * 3.1f) * 2f : 0f;
                head.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 3f + nod, Mathf.Sin(t * 0.43f) * 6f, 0f);
            }
            if (body)   // gentle sway while talking, back to rest afterwards
                body.localRotation = Quaternion.Slerp(body.localRotation,
                    speaking ? m_BodyRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f) * 1.5f) : m_BodyRot, 1f - Mathf.Exp(-6f * Time.deltaTime));
            UpdateGlow(speaking);

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

        void UpdateGlow(bool speaking)
        {
            if (!speakGlow) return;
            m_Glow = Mathf.MoveTowards(m_Glow, speaking ? 1f : 0f, Time.deltaTime * 3f);
            bool on = m_Glow > 0.01f;
            if (speakGlow.enabled != on) speakGlow.enabled = on;
            if (!on) return;
            var gt = speakGlow.transform;
            gt.localScale = Vector3.one * (0.55f * Mathf.SmoothStep(0f, 1f, m_Glow) * (1f + Mathf.Sin(Time.time * 3f) * 0.06f));
            var gm = GameManager.Instance;
            if (gm && gm.Rig)
            {
                var to = gt.position - gm.Rig.Head.position;
                if (to.sqrMagnitude > 0.0001f) gt.rotation = Quaternion.LookRotation(to);
            }
        }
    }
}
