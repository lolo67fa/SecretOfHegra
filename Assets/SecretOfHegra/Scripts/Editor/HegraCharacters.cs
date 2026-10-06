using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Placeholder characters built from primitives with the palette's costume colors. Each gets an
    /// <see cref="NpcFigure"/> for procedural idle/gesture animation. Replace with rigged models later.
    /// </summary>
    public static class HegraCharacters
    {
        public class Figure
        {
            public GameObject root;
            public Transform body, head, leftArm, rightArm, leftHand, rightHand;
            public NpcFigure npc;
        }

        public static Figure Build(string name, Transform parent, Vector3 pos, float yaw, Material robe, Material skin, float height = 1.72f, Material sleeves = null)
        {
            var f = new Figure();
            f.root = HegraKit.Group(name, parent, pos, yaw);
            var root = f.root.transform;
            float s = height / 1.72f;

            var body = new GameObject("Body").transform;
            body.SetParent(root, false);
            f.body = body;
            // robe: tapered by stacking a capsule over a cylinder skirt
            HegraKit.Prim(PrimitiveType.Cylinder, body, "Skirt", new Vector3(0, 0.42f * s, 0), new Vector3(0.5f * s, 0.42f * s, 0.42f * s), robe);
            HegraKit.Prim(PrimitiveType.Capsule, body, "Torso", new Vector3(0, 1.1f * s, 0), new Vector3(0.42f * s, 0.42f * s, 0.32f * s), robe);
            HegraKit.Prim(PrimitiveType.Sphere, body, "Shoulders", new Vector3(0, 1.36f * s, 0), new Vector3(0.48f * s, 0.16f * s, 0.3f * s), robe);
            HegraKit.Prim(PrimitiveType.Cylinder, body, "Neck", new Vector3(0, 1.47f * s, 0), new Vector3(0.1f * s, 0.05f * s, 0.1f * s), skin);

            var head = new GameObject("Head").transform;
            head.SetParent(root, false);
            head.localPosition = new Vector3(0, 1.6f * s, 0);
            f.head = head;
            HegraKit.Prim(PrimitiveType.Sphere, head, "Face", Vector3.zero, new Vector3(0.21f, 0.25f, 0.22f) * s, skin);
            HegraKit.Prim(PrimitiveType.Sphere, head, "EyeL", new Vector3(-0.045f, 0.02f, 0.1f) * s, Vector3.one * 0.025f * s, HegraKit.Unlit("M_Eye", new Color(0.08f, 0.06f, 0.06f)));
            HegraKit.Prim(PrimitiveType.Sphere, head, "EyeR", new Vector3(0.045f, 0.02f, 0.1f) * s, Vector3.one * 0.025f * s, HegraKit.Unlit("M_Eye", new Color(0.08f, 0.06f, 0.06f)));

            f.leftArm = Arm(root, "ArmL", new Vector3(-0.24f * s, 1.36f * s, 0), sleeves ? sleeves : robe, skin, s, out f.leftHand);
            f.rightArm = Arm(root, "ArmR", new Vector3(0.24f * s, 1.36f * s, 0), sleeves ? sleeves : robe, skin, s, out f.rightHand);

            f.npc = f.root.AddComponent<NpcFigure>();
            f.npc.body = body;
            f.npc.head = head;
            f.npc.leftArm = f.leftArm;
            f.npc.rightArm = f.rightArm;

            // simple capsule collider so rays and the player do not pass through
            var col = f.root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.9f * s, 0);
            col.height = 1.8f * s;
            col.radius = 0.28f * s;
            return f;
        }

        static Transform Arm(Transform root, string name, Vector3 shoulder, Material sleeve, Material skin, float s, out Transform hand)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(root, false);
            pivot.localPosition = shoulder;
            pivot.localEulerAngles = new Vector3(0, 0, name.EndsWith("L") ? -8f : 8f);
            HegraKit.Prim(PrimitiveType.Capsule, pivot, "Sleeve", new Vector3(0, -0.28f * s, 0), new Vector3(0.12f, 0.3f, 0.12f) * s, sleeve);
            var h = HegraKit.Prim(PrimitiveType.Sphere, pivot, "Hand", new Vector3(0, -0.6f * s, 0), new Vector3(0.08f, 0.1f, 0.06f) * s, skin);
            hand = h.transform;
            return pivot;
        }

        // ───────────── costume details ─────────────

        public static void Veil(Figure f, Material mat)
        {
            HegraKit.Prim(PrimitiveType.Sphere, f.head, "Veil", new Vector3(0, 0.03f, -0.025f), new Vector3(0.26f, 0.3f, 0.27f), mat);
            HegraKit.Prim(PrimitiveType.Capsule, f.head, "VeilDrape", new Vector3(0, -0.2f, -0.07f), new Vector3(0.3f, 0.22f, 0.16f), mat);
        }

        public static void Headcloth(Figure f, Material mat)
        {
            HegraKit.Prim(PrimitiveType.Sphere, f.head, "Headcloth", new Vector3(0, 0.05f, -0.02f), new Vector3(0.25f, 0.24f, 0.26f), mat);
            HegraKit.Prim(PrimitiveType.Capsule, f.head, "ClothTail", new Vector3(0, -0.16f, -0.1f), new Vector3(0.22f, 0.16f, 0.1f), mat);
        }

        public static void Headband(Figure f, Material mat)
        {
            HegraKit.Prim(PrimitiveType.Cylinder, f.head, "Headband", new Vector3(0, 0.07f, 0), new Vector3(0.235f, 0.015f, 0.235f), mat);
            HegraKit.Prim(PrimitiveType.Sphere, f.head, "Hair", new Vector3(0, 0.08f, -0.02f), new Vector3(0.2f, 0.14f, 0.2f), HegraKit.Lit("M_Hair", new Color(0.12f, 0.08f, 0.06f)));
        }

        public static void Jewelry(Figure f, Material bronze)
        {
            HegraKit.Prim(PrimitiveType.Cylinder, f.body, "Necklace", new Vector3(0, 1.42f, 0.02f), new Vector3(0.3f, 0.012f, 0.24f), bronze);
            HegraKit.Prim(PrimitiveType.Sphere, f.body, "Pendant", new Vector3(0, 1.36f, 0.15f), Vector3.one * 0.045f, bronze);
            HegraKit.Prim(PrimitiveType.Cylinder, f.leftHand, "Bracelet", new Vector3(0, 0.5f, 0), new Vector3(1.2f, 0.25f, 1.4f), bronze);
            HegraKit.Prim(PrimitiveType.Sphere, f.head, "EarringL", new Vector3(-0.11f, -0.04f, 0), Vector3.one * 0.025f, bronze);
            HegraKit.Prim(PrimitiveType.Sphere, f.head, "EarringR", new Vector3(0.11f, -0.04f, 0), Vector3.one * 0.025f, bronze);
        }

        public static void Apron(Figure f, Material leather)
        {
            HegraKit.Prim(PrimitiveType.Cube, f.body, "Apron", new Vector3(0, 0.75f, 0.2f), new Vector3(0.42f, 0.75f, 0.03f), leather);
        }

        public static void Belt(Figure f, Material leather, float y = 0.95f)
        {
            HegraKit.Prim(PrimitiveType.Cylinder, f.body, "Belt", new Vector3(0, y, 0), new Vector3(0.47f, 0.03f, 0.37f), leather);
        }

        public static void Stripes(Figure f, Material stripe)
        {
            HegraKit.Prim(PrimitiveType.Cube, f.body, "StripeL", new Vector3(-0.1f, 0.85f, 0.19f), new Vector3(0.04f, 1.1f, 0.02f), stripe);
            HegraKit.Prim(PrimitiveType.Cube, f.body, "StripeR", new Vector3(0.1f, 0.85f, 0.19f), new Vector3(0.04f, 1.1f, 0.02f), stripe);
        }

        public static void Mantle(Figure f, Material mantle)
        {
            HegraKit.Prim(PrimitiveType.Cube, f.body, "Mantle", new Vector3(0.02f, 1.05f, -0.12f), new Vector3(0.5f, 0.75f, 0.06f), mantle, new Vector3(4f, 0, -6f));
            HegraKit.Prim(PrimitiveType.Capsule, f.body, "MantleShoulder", new Vector3(-0.17f, 1.33f, 0), new Vector3(0.2f, 0.12f, 0.33f), mantle, new Vector3(0, 0, 70f));
        }

        public static void Spear(Figure f, Material wood, Material bronze)
        {
            var s = HegraKit.Group("Spear", f.rightHand, Vector3.zero).transform;
            s.localEulerAngles = new Vector3(0, 0, 0);
            HegraKit.Prim(PrimitiveType.Cylinder, s, "Shaft", new Vector3(0, 6.5f, 0), new Vector3(0.35f, 9f, 0.35f), wood);
            HegraKit.Prim(PrimitiveType.Cube, s, "Tip", new Vector3(0, 16f, 0), new Vector3(0.7f, 2.2f, 0.25f), bronze, new Vector3(0, 0, 45f));
        }

        public static void Shield(Figure f, Material leather, Material bronze)
        {
            HegraKit.Prim(PrimitiveType.Cylinder, f.leftHand, "Shield", new Vector3(-0.9f, 0.6f, 0.4f), new Vector3(6f, 0.25f, 7f), leather, new Vector3(0, 0, 90f));
            HegraKit.Prim(PrimitiveType.Sphere, f.leftHand, "Boss", new Vector3(-1.2f, 0.6f, 0.4f), new Vector3(1.6f, 1.2f, 1.6f), bronze);
        }

        public static void Lyre(Figure f, Material wood, Material bronze)
        {
            var l = HegraKit.Group("Lyre", f.leftHand, new Vector3(0, 1.6f, 1.6f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, l, "Base", new Vector3(0, -1.5f, 0), new Vector3(3.2f, 1.2f, 0.8f), wood);
            HegraKit.Prim(PrimitiveType.Cube, l, "ArmL", new Vector3(-1.3f, 1f, 0), new Vector3(0.5f, 4.4f, 0.6f), wood, new Vector3(0, 0, 10f));
            HegraKit.Prim(PrimitiveType.Cube, l, "ArmR", new Vector3(1.3f, 1f, 0), new Vector3(0.5f, 4.4f, 0.6f), wood, new Vector3(0, 0, -10f));
            HegraKit.Prim(PrimitiveType.Cube, l, "Yoke", new Vector3(0, 3.1f, 0), new Vector3(3.6f, 0.4f, 0.5f), wood);
            for (int i = 0; i < 5; i++)
                HegraKit.Prim(PrimitiveType.Cube, l, "String" + i, new Vector3(-0.8f + i * 0.4f, 0.8f, 0), new Vector3(0.05f, 4.4f, 0.05f), bronze);
        }

        public static GameObject Camel(Transform parent, Vector3 pos, float yaw, HegraMats m)
        {
            var c = HegraKit.Group("Camel", parent, pos, yaw);
            var t = c.transform;
            HegraKit.Prim(PrimitiveType.Capsule, t, "Body", new Vector3(0, 1.35f, 0), new Vector3(0.75f, 0.85f, 0.7f), m.camel, new Vector3(90, 0, 0));
            HegraKit.Prim(PrimitiveType.Sphere, t, "Hump", new Vector3(0, 1.85f, -0.05f), new Vector3(0.6f, 0.55f, 0.75f), m.camel);
            HegraKit.Prim(PrimitiveType.Cube, t, "Saddlecloth", new Vector3(0, 1.72f, 0.4f), new Vector3(0.8f, 0.05f, 0.5f), m.clothRed, new Vector3(10, 0, 0));
            var neck = HegraKit.Prim(PrimitiveType.Capsule, t, "Neck", new Vector3(0, 1.75f, 0.95f), new Vector3(0.25f, 0.5f, 0.25f), m.camel, new Vector3(35, 0, 0));
            HegraKit.Prim(PrimitiveType.Capsule, t, "Head", new Vector3(0, 2.15f, 1.3f), new Vector3(0.22f, 0.28f, 0.22f), m.camel, new Vector3(80, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.22f : 0.22f;
                float z = i < 2 ? 0.55f : -0.55f;
                HegraKit.Prim(PrimitiveType.Cylinder, t, "Leg" + i, new Vector3(x, 0.5f, z), new Vector3(0.13f, 0.5f, 0.13f), m.camel);
            }
            var col = c.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.2f, 0.2f);
            col.size = new Vector3(0.8f, 2.4f, 2.4f);
            var idle = c.AddComponent<NpcFigure>();
            idle.facePlayer = false;
            idle.head = null;
            idle.body = t.Find("Body");
            return c;
        }
    }
}
