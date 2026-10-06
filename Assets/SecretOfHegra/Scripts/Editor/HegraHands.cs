using System.Collections.Generic;
using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    /// <summary>Builds the traveler's first-person hands and sleeves on both controllers.</summary>
    public static class HegraHands
    {
        public static void Build(Transform left, Transform right, HegraMats m)
        {
            if (!left || !right) return;
            var hands = new List<Transform>();
            var female = new List<GameObject>();
            var male = new List<GameObject>();
            var sleeves = new List<GameObject>();
            foreach (var (ctrl, side) in new[] { (left, -1f), (right, 1f) })
            {
                foreach (Transform child in ctrl)
                    if (child.name.EndsWith("Controller Visual")) child.gameObject.SetActive(false);
                var root = new GameObject("TravelerHand").transform;
                root.SetParent(ctrl, false);
                // hand sits slightly behind the controller grip; sleeve extends back toward the elbow
                root.localPosition = new Vector3(0, -0.015f, -0.03f);
                var hand = HegraKit.Group("Hand", root, Vector3.zero).transform;
                HegraKit.Prim(PrimitiveType.Sphere, hand, "Palm", new Vector3(0, 0, 0.02f), new Vector3(0.075f, 0.035f, 0.09f), m.skinA);
                HegraKit.Prim(PrimitiveType.Capsule, hand, "Thumb", new Vector3(-side * 0.035f, 0.01f, 0.04f), new Vector3(0.022f, 0.03f, 0.022f), m.skinA, new Vector3(70, side * 30f, 0));
                HegraKit.Prim(PrimitiveType.Capsule, hand, "Fingers", new Vector3(side * 0.005f, -0.012f, 0.07f), new Vector3(0.07f, 0.035f, 0.03f), m.skinA, new Vector3(0, 0, 90));
                hands.Add(hand);
                sleeves.Add(HegraKit.Prim(PrimitiveType.Cylinder, root, "Sleeve", new Vector3(0, 0, -0.085f), new Vector3(0.058f, 0.045f, 0.058f), m.linen, new Vector3(90, 0, 0)));
                sleeves.Add(HegraKit.Prim(PrimitiveType.Cylinder, root, "MantleCuff", new Vector3(0, 0, -0.125f), new Vector3(0.066f, 0.012f, 0.066f), m.indigo, new Vector3(90, 0, 0)));
                female.Add(HegraKit.Prim(PrimitiveType.Cylinder, root, "Bracelet", new Vector3(0, 0, -0.04f), new Vector3(0.06f, 0.006f, 0.06f), m.bronze, new Vector3(90, 0, 0)));
                male.Add(HegraKit.Prim(PrimitiveType.Cylinder, root, "WristBand", new Vector3(0, 0, -0.045f), new Vector3(0.062f, 0.014f, 0.062f), m.leather, new Vector3(90, 0, 0)));
            }
            var comp = left.root.gameObject.AddComponent<TravelerHands>();
            comp.hands = hands.ToArray();
            comp.femaleOnly = female.ToArray();
            comp.maleOnly = male.ToArray();
            comp.sleeves = sleeves.ToArray();
        }
    }
}
