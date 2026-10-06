using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Guided engraving: for each letter a short stroke (start + end checkpoint) is laid right-to-left
    /// across the shard. Touching the glowing checkpoints in order with the chisel tip — or simply the
    /// controller — carves the name. Checkpoints are forgiving spheres; there is no handwriting recognition.
    /// </summary>
    public class ShardEngraver : MonoBehaviour
    {
        public NameShard shard;
        public Transform surface;          // local +X = right, +Y = up on the shard face
        public float faceWidth = 0.22f;
        public float strokeHeight = 0.045f;
        public float radius = 0.05f;
        public Transform chiselTip;
        public Material pointMaterial;
        public Material doneMaterial;
        public LineRenderer guide;

        public event Action Finished;

        readonly List<Transform> m_Points = new List<Transform>();
        int m_Next;
        bool m_Active;

        public bool Active => m_Active;
        public float Progress => m_Points.Count == 0 ? 0f : (float)m_Next / m_Points.Count;

        public void Begin(int letters)
        {
            Clear();
            letters = Mathf.Clamp(letters, 1, NameRules.MaxLength);
            float step = faceWidth / letters;
            for (int i = 0; i < letters; i++)
            {
                // right-to-left like Nabataean writing
                float x = faceWidth * 0.5f - step * (i + 0.5f);
                m_Points.Add(MakePoint(new Vector3(x, strokeHeight * 0.5f, -0.012f)));
                m_Points.Add(MakePoint(new Vector3(x - step * 0.15f, -strokeHeight * 0.5f, -0.012f)));
            }
            m_Next = 0;
            m_Active = true;
            UpdateVisuals();
        }

        Transform MakePoint(Vector3 local)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(p.GetComponent<Collider>());
            p.name = "CarvePoint";
            p.transform.SetParent(surface, false);
            p.transform.localPosition = local;
            p.transform.localScale = Vector3.one * 0.018f;
            var r = p.GetComponent<Renderer>();
            r.sharedMaterial = pointMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return p.transform;
        }

        public void Clear()
        {
            foreach (var p in m_Points) if (p) Destroy(p.gameObject);
            m_Points.Clear();
            m_Active = false;
            if (guide) guide.positionCount = 0;
        }

        /// <summary>Accessible alternative: carve every remaining stroke automatically.</summary>
        public void CarveAll()
        {
            if (!m_Active) return;
            while (m_Next < m_Points.Count) Hit();
        }

        void Update()
        {
            if (!m_Active || m_Next >= m_Points.Count) return;
            var target = m_Points[m_Next].position;
            if (Near(chiselTip, target)) { Hit(); return; }
            var gm = GameManager.Instance;
            if (gm && gm.Rig && (Near(gm.Rig.LeftController, target) || Near(gm.Rig.RightController, target))) Hit();
        }

        bool Near(Transform t, Vector3 p) => t && t.gameObject.activeInHierarchy && (t.position - p).sqrMagnitude < radius * radius;

        void Hit()
        {
            var p = m_Points[m_Next];
            p.GetComponent<Renderer>().sharedMaterial = doneMaterial;
            p.localScale = Vector3.one * 0.01f;
            HegraAudio.PlayAt(Sfx.Carve, p.position, 0.8f);
            m_Next++;
            if (shard) shard.SetEngraveProgress(Progress);
            UpdateVisuals();
            if (m_Next >= m_Points.Count)
            {
                m_Active = false;
                Clear();
                Finished?.Invoke();
            }
        }

        void UpdateVisuals()
        {
            for (int i = 0; i < m_Points.Count; i++)
            {
                if (i < m_Next) continue;
                float s = i == m_Next ? 0.026f + Mathf.Sin(Time.time * 6f) * 0.002f : 0.014f;
                m_Points[i].localScale = Vector3.one * s;
            }
            if (guide && m_Next < m_Points.Count)
            {
                // highlight the current stroke
                int a = m_Next - (m_Next % 2);
                guide.positionCount = 2;
                guide.SetPosition(0, m_Points[a].position);
                guide.SetPosition(1, m_Points[Mathf.Min(a + 1, m_Points.Count - 1)].position);
            }
        }

        void LateUpdate()
        {
            if (m_Active && m_Next < m_Points.Count)
            {
                var cur = m_Points[m_Next];
                cur.localScale = Vector3.one * (0.026f + Mathf.Sin(Time.time * 6f) * 0.003f);
            }
        }
    }
}
