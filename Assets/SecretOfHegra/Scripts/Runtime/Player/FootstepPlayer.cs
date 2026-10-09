using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Soft footsteps on sand while the player walks with the thumbstick (teleports already make their own step).
    /// Sits on the rig and counts the distance the rig itself travels, so looking around never triggers a step.
    /// </summary>
    public class FootstepPlayer : MonoBehaviour
    {
        public float stride = 0.75f;
        public float volume = 0.5f;

        PlayerRig m_Rig;
        Vector3 m_Last;
        float m_Travelled;

        void Start()
        {
            m_Rig = GetComponent<PlayerRig>();
            m_Last = transform.position;
        }

        void Update()
        {
            var p = transform.position;
            var d = p - m_Last; d.y = 0f;
            m_Last = p;
            float step = d.magnitude;
            if (step > 1.2f) { m_Travelled = 0f; return; }   // a teleport or a scene placement, not a walk
            m_Travelled += step;
            if (m_Travelled < stride) return;
            m_Travelled = 0f;
            var head = m_Rig && m_Rig.Head ? m_Rig.Head.position : p + Vector3.up * 1.6f;
            HegraAudio.PlayAt(Sfx.Footstep, new Vector3(head.x, p.y + 0.05f, head.z), volume);
        }
    }
}
