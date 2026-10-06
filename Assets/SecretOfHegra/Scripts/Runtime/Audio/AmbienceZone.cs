using System.Collections.Generic;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>Music + ambience beds for one stop. The nearest zone containing the listener wins.</summary>
    public class AmbienceZone : MonoBehaviour
    {
        public StopId stop;
        public float radius = 14f;
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        public AudioClip ambience;
        [Range(0f, 1f)] public float ambienceVolume = 0.6f;

        static readonly List<AmbienceZone> s_All = new List<AmbienceZone>();

        void OnEnable() => s_All.Add(this);
        void OnDisable() => s_All.Remove(this);

        public static AmbienceZone Find(Vector3 position)
        {
            AmbienceZone best = null;
            float bestDist = float.MaxValue;
            foreach (var z in s_All)
            {
                var d = Vector3.Distance(position, z.transform.position);
                if (d <= z.radius && d < bestDist)
                {
                    best = z;
                    bestDist = d;
                }
            }
            return best;
        }

        public static AmbienceZone ForStop(StopId stop)
        {
            foreach (var z in s_All) if (z.stop == stop) return z;
            return null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
