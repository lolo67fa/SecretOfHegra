using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// A slowly turning golden diamond with a soft halo that floats above the character the player should talk to
    /// next (whenever the current objective is a person). It hides while that character speaks and once the
    /// player is standing with them. Also lights up any speaking character the player points a controller at.
    /// </summary>
    public class GuideMarker : MonoBehaviour
    {
        public Transform diamond;
        public Renderer halo;
        public float hideWithin = 2.2f;

        float m_Show;
        NpcFigure m_Pointed;

        void LateUpdate()
        {
            var gm = GameManager.Instance;
            if (!gm || !gm.Rig) return;
            UpdatePointer(gm);

            var target = ObjectiveBeacon.Current;
            var npc = target ? target.GetComponentInParent<NpcFigure>() : null;
            bool want = npc && !string.IsNullOrEmpty(npc.speakerId) && gm.InGame && !gm.MenuOpen && !(gm.Dialogue && gm.Dialogue.IsPlaying);
            float dist = 0f;
            if (npc)
            {
                var d = npc.transform.position - gm.Rig.Head.position; d.y = 0f;
                dist = d.magnitude;
                if (dist < hideWithin) want = false;
            }
            m_Show = Mathf.MoveTowards(m_Show, want ? 1f : 0f, Time.deltaTime * 2.5f);
            bool on = m_Show > 0.01f;
            if (diamond.gameObject.activeSelf != on) diamond.gameObject.SetActive(on);
            if (halo && halo.enabled != on) halo.enabled = on;
            if (!on || !npc) return;

            float t = Time.time;
            float size = Mathf.Clamp(dist / 3f, 1f, 3f) * Mathf.SmoothStep(0f, 1f, m_Show);
            transform.position = npc.DialogueAnchorPosition + Vector3.up * (0.38f * size + Mathf.Sin(t * 2.2f) * 0.05f);
            diamond.localRotation = Quaternion.Euler(0f, t * 70f, 0f);
            diamond.localScale = new Vector3(0.15f, 0.22f, 0.15f) * size;
            if (halo)
            {
                var ht = halo.transform;
                ht.localScale = Vector3.one * 0.8f * size * (1f + Mathf.Sin(t * 3f) * 0.08f);
                var to = ht.position - gm.Rig.Head.position;
                if (to.sqrMagnitude > 0.0001f) ht.rotation = Quaternion.LookRotation(to);
            }
        }

        /// <summary>Pointing a controller at a character who has something to say makes them glow softly.</summary>
        void UpdatePointer(GameManager gm)
        {
            NpcFigure hit = null;
            foreach (var c in new[] { gm.Rig.RightController, gm.Rig.LeftController })
            {
                if (!c || hit) continue;
                if (Physics.Raycast(c.position, c.forward, out var h, 14f, ~0, QueryTriggerInteraction.Ignore))
                {
                    var n = h.collider.GetComponentInParent<NpcFigure>();
                    if (n && !string.IsNullOrEmpty(n.speakerId)) hit = n;
                }
            }
            if (hit == m_Pointed) return;
            if (m_Pointed) m_Pointed.Highlighted = false;
            m_Pointed = hit;
            if (hit) { hit.Highlighted = true; Haptics.Both(0.1f, 0.03f); }
        }
    }
}
