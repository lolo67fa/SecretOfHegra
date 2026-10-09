using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Background life: a townsperson strolling back and forth along a few points (pausing at each, as if looking
    /// at goods), or a child running a small loop. Purely visual; it never blocks the player (no solid collider).
    /// </summary>
    public class NpcWalker : MonoBehaviour
    {
        public Vector3[] points;
        public float speed = 0.8f;
        public float pause = 2.5f;
        public bool loop;          // run the points as a circuit instead of back and forth
        public float bob = 0.02f;  // step bounce (a child hops more)

        NpcFigure m_Figure;
        int m_Index, m_Dir = 1;
        float m_Wait;
        float m_Phase;
        float m_BaseY;

        void Awake()
        {
            m_Figure = GetComponent<NpcFigure>();
            if (m_Figure) m_Figure.facePlayer = false;
            m_Phase = Random.value * 10f;
            m_BaseY = transform.position.y;
            m_Wait = Random.Range(0f, pause);
        }

        void Update()
        {
            if (points == null || points.Length < 2) return;
            if (m_Wait > 0f) { m_Wait -= Time.deltaTime; return; }
            var target = points[m_Index];
            var pos = transform.position;
            var to = target - pos; to.y = 0f;
            float step = speed * Time.deltaTime;
            if (to.magnitude <= step)
            {
                pos.x = target.x; pos.z = target.z;
                Advance();
                m_Wait = pause * Random.Range(0.6f, 1.4f);
            }
            else
            {
                pos += to.normalized * step;
                var goal = Quaternion.LookRotation(to);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, 220f * Time.deltaTime);
            }
            m_Phase += Time.deltaTime * speed * 6f;
            pos.y = m_BaseY + (m_Wait > 0f ? 0f : Mathf.Abs(Mathf.Sin(m_Phase)) * bob);
            transform.position = pos;
        }

        void Advance()
        {
            if (loop) { m_Index = (m_Index + 1) % points.Length; return; }
            if (m_Index + m_Dir >= points.Length || m_Index + m_Dir < 0) m_Dir = -m_Dir;
            m_Index += m_Dir;
        }
    }
}
