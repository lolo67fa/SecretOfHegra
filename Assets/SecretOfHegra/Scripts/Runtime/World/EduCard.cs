using System.Collections.Generic;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>Optional educational card (museum/school mode). Text comes from the localization table.</summary>
    public class EduCard : MonoBehaviour
    {
        public string titleKey;
        public string bodyKey;

        static readonly List<EduCard> s_All = new List<EduCard>();
        static bool s_Visible;

        void Awake() => s_All.Add(this);
        void OnDestroy() => s_All.Remove(this);
        void Start() => gameObject.SetActive(s_Visible);

        public static void SetVisible(bool visible)
        {
            s_Visible = visible;
            foreach (var c in s_All) if (c) c.gameObject.SetActive(visible);
        }
    }
}
