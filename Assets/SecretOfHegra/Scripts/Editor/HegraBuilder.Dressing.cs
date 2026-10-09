using System.Collections.Generic;
using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Set dressing that makes the camp, the market and the valley feel lived in: palms, bushes, rocks, carpets and
    /// cushions, stalls with goods, barrels and jars, more campfires, sitting and resting camels, Nabataean tomb
    /// facades and dunes on the horizon, and background people (sitting by fires, strolling, a child, a guard).
    /// Placement is seeded (the same every build), keeps the walking route and the story spots clear, and every
    /// static cluster is merged into one mesh per material so the headset draws it cheaply.
    /// </summary>
    public static partial class HegraBuilder
    {
        static System.Random s_DressRnd;
        static Mesh s_LowSphere, s_LowCyl;
        static (Mesh high, Mesh low) s_Pebble, s_Pebble2;
        static Material s_PalmTrunk, s_PalmFrond, s_Dates, s_DryBush, s_SpiceRed, s_SpiceBrown, s_Charcoal;

        static float Rf(float a, float b) => a + (float)s_DressRnd.NextDouble() * (b - a);
        static int Ri(int a, int b) => s_DressRnd.Next(a, b);

        // ───────────── entry ─────────────

        static void BuildDressing(Transform world, HegraAudioAssets.Set audio)
        {
            s_DressRnd = new System.Random(9001);
            s_LowSphere = LowSphereMesh();
            s_LowCyl = LowCylinderMesh(10);
            s_Pebble = RockMeshes("Pebble", 211, 12, 8, 0.25f);
            s_Pebble2 = RockMeshes("Pebble2", 317, 12, 8, 0.3f);
            s_PalmTrunk = HegraKit.Lit("M_PalmTrunk", new Color(0.42f, 0.32f, 0.22f), null, 0.08f);
            s_PalmFrond = HegraKit.Lit("M_PalmFrond", new Color(0.2f, 0.36f, 0.16f), null, 0.12f);
            s_Dates = HegraKit.Lit("M_Dates", new Color(0.42f, 0.17f, 0.08f), null, 0.5f);
            s_DryBush = HegraKit.Lit("M_DryBush", new Color(0.5f, 0.42f, 0.28f), null, 0.05f);
            s_SpiceRed = HegraKit.Lit("M_SpiceRed", new Color(0.72f, 0.2f, 0.08f), null, 0.05f);
            s_SpiceBrown = HegraKit.Lit("M_SpiceBrown", new Color(0.45f, 0.28f, 0.12f), null, 0.05f);
            s_Charcoal = HegraKit.Lit("M_Charcoal", new Color(0.12f, 0.1f, 0.09f), null, 0.05f);

            var root = HegraKit.Group("Dressing", world, Vector3.zero).transform;
            var life = HegraKit.Group("Life", root, Vector3.zero).transform;   // animated: never merged

            CampOutskirts(root, life, audio);
            MarketOutskirts(root, life);
            RouteMargins(root);
            Horizon(root);
        }

        // ───────────── keep-clear rules ─────────────

        static readonly (Vector3 c, float r)[] s_StoryAreas =
        {
            (new Vector3(0, 0, 0), 7.5f),     // camp lessons
            (new Vector3(0, 0, 28.5f), 8.5f), // market plaza
            (new Vector3(22, 0, 46), 11f),    // theater + Elephant Rock
            (new Vector3(50, 0, 58), 12f),    // Jabal Ithlib
            (new Vector3(78, 0, 44), 12f),    // Qasr al-Bint
            (new Vector3(100, 0, 18), 13f),   // Qasr al-Farid
        };

        static List<Vector3> RoutePoints()
        {
            var pts = new List<Vector3> { CampPos };
            foreach (var d in s_StopDefs) pts.Add(d.pos);
            return pts;
        }

        static float DistanceToRoute(Vector3 p)
        {
            var pts = RoutePoints();
            float best = float.MaxValue;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var a = pts[i]; var b = pts[i + 1];
                var ab = b - a; ab.y = 0;
                var ap = p - a; ap.y = 0;
                float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
                var q = a + ab * t;
                var d = p - q; d.y = 0;
                best = Mathf.Min(best, d.magnitude);
            }
            return best;
        }

        /// <summary>True when a prop of the given radius may stand here (off the path and out of story spots).</summary>
        static bool Clear(Vector3 p, float radius, float pathMargin = 3.2f)
        {
            if (DistanceToRoute(p) < pathMargin + radius) return false;
            foreach (var a in s_StoryAreas)
            {
                var d = p - a.c; d.y = 0;
                if (d.magnitude < a.r + radius) return false;
            }
            return true;
        }

        // ───────────── areas ─────────────

        static void CampOutskirts(Transform root, Transform life, HegraAudioAssets.Set audio)
        {
            var g = HegraKit.Group("Camp_Dressing", root, Vector3.zero).transform;

            // a second tent row with carpets and cushions (west), sleeping area (south)
            foreach (var (p, yaw) in new[] { (new Vector3(-11f, 0, 6f), 80f), (new Vector3(-12f, 0, -3f), 100f), (new Vector3(9f, 0, -8f), -40f), (new Vector3(-3f, 0, -12f), 10f) })
            {
                Tent(g, p, yaw, Rf(2.8f, 3.4f), Rf(2f, 2.4f), Rf(3.6f, 4.4f));
                var front = p + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, 3.4f);
                CarpetWithCushions(g, front, yaw);
            }

            // the oasis corner: palms around a small well
            var oasis = new Vector3(10f, 0, 10f);
            Well(g, oasis);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 51f * Mathf.Deg2Rad;
                Palm(g, oasis + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Rf(2.6f, 5.5f), Rf(4.5f, 7.5f));
            }

            // supplies by the tents
            for (int i = 0; i < 6; i++) GoodsCluster(g, new Vector3(Rf(-14f, -8f), 0, Rf(-8f, 12f)));
            for (int i = 0; i < 3; i++) GoodsCluster(g, new Vector3(Rf(6f, 13f), 0, Rf(-12f, -4f)));

            // two more campfires, each with people sitting round it; a child plays by the first one
            var fireA = new Vector3(-8f, 0, 13f);
            var fireB = new Vector3(6.5f, 0, -12f);
            Campfire(life, fireA, audio);
            Campfire(life, fireB, audio);
            SittingCircle(life, fireA, 3, 0);
            SittingCircle(life, fireB, 2, 1);
            Child(life, fireA, 2.6f);

            // camels resting at the edge of camp
            SittingCamel(life, new Vector3(-13f, 0, 13f), 120f);
            SittingCamel(life, new Vector3(13f, 0, -2f), 250f);
            HegraCharacters.Camel(life, new Vector3(-15f, 0, 9f), 70f, M).name = "CamelStanding";

            // a guard watching the road north
            var guard = HegraCharacters.Build("Guard", life, new Vector3(3.6f, 0, 11.5f), 200f, M.earth, M.skinB, 1.8f);
            HegraCharacters.Headcloth(guard, M.indigo);
            HegraCharacters.Spear(guard, M.wood, M.bronze);
            HegraCharacters.Belt(guard, M.leather);
            guard.npc.facePlayer = false;

            Scatter(g, new Vector3(0, 0, 0), 9f, 22f, 26, true);
            CombineByMaterial(g, "Dress_Camp", true);
            HegraKit.MarkStatic(g.gameObject);
        }

        static void MarketOutskirts(Transform root, Transform life)
        {
            var g = HegraKit.Group("Market_Dressing", root, Vector3.zero).transform;
            var mc = MarketPos;

            // an outer ring of stalls with goods, facing the plaza
            var stalls = new[]
            {
                (new Vector3(-9f, 0, 22f), 90f, M.clothGreen), (new Vector3(-9.5f, 0, 28f), 90f, M.clothSaffron),
                (new Vector3(-9f, 0, 34f), 90f, M.clothRed), (new Vector3(9.5f, 0, 21.5f), -90f, M.indigo),
                (new Vector3(10f, 0, 34.5f), -90f, M.clothGreen),
            };
            int k = 0;
            foreach (var (p, yaw, awning) in stalls)
            {
                var s = Stall(g, "OuterStall", p, yaw, awning, 2.4f);
                StallGoods(s, k++);
                var trader = HegraCharacters.Build("Trader", life, s.TransformPoint(new Vector3(0, 0, -0.15f)), yaw, k % 2 == 0 ? M.clothRed : M.earth, k % 2 == 0 ? M.skinA : M.skinB, Rf(1.6f, 1.78f));
                if (k % 2 == 0) HegraCharacters.Headcloth(trader, M.linen); else HegraCharacters.Veil(trader, M.clothSaffron);
                trader.npc.idleGesture = k % 3 == 0 ? NpcGesture.Point : NpcGesture.Idle;
                trader.npc.faceDistance = 5f;
            }

            // fabric lines and incense between the stalls
            FabricLine(g, new Vector3(-11f, 0, 23.5f), new Vector3(-11f, 0, 29.5f));
            FabricLine(g, new Vector3(11.3f, 0, 25.5f), new Vector3(11.3f, 0, 31.5f));
            IncenseBurner(life, new Vector3(-8.4f, 0, 25f));
            IncenseBurner(life, new Vector3(8.6f, 0, 28f));
            IncenseBurner(life, new Vector3(-8.4f, 0, 31f));

            for (int i = 0; i < 10; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                GoodsCluster(g, new Vector3(side * Rf(11.5f, 15f), 0, Rf(18f, 38f)));
            }
            for (int i = 0; i < 4; i++) Palm(g, new Vector3((i % 2 == 0 ? -1 : 1) * Rf(13f, 17f), 0, Rf(16f, 40f)), Rf(5f, 8f));

            // people strolling along both edges of the plaza, pausing at the stalls
            Walker(life, new[] { new Vector3(-7.4f, 0, 19f), new Vector3(-7.4f, 0, 26f), new Vector3(-7.4f, 0, 36f) }, M.clothGreen, M.skinA, 0.75f);
            Walker(life, new[] { new Vector3(7.6f, 0, 37f), new Vector3(7.6f, 0, 29f), new Vector3(7.6f, 0, 20f) }, M.indigo, M.skinB, 0.7f);
            Walker(life, new[] { new Vector3(-12f, 0, 20f), new Vector3(-12f, 0, 38f) }, M.mantleRed, M.skinB, 0.85f);

            CombineByMaterial(g, "Dress_Market", true);
            HegraKit.MarkStatic(g.gameObject);
        }

        static void RouteMargins(Transform root)
        {
            var pts = RoutePoints();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var g = HegraKit.Group("Route_Dressing_" + i, root, Vector3.zero).transform;
                var a = pts[i]; var b = pts[i + 1];
                var dir = b - a; dir.y = 0;
                float len = dir.magnitude;
                var fwd = dir / len;
                var side = Vector3.Cross(Vector3.up, fwd);
                for (float t = 2f; t < len; t += Rf(3f, 5.5f))
                {
                    foreach (var sgn in new[] { -1f, 1f })
                    {
                        var p = a + fwd * t + side * sgn * Rf(4.5f, 11f);
                        int pick = Ri(0, 10);
                        if (pick < 4) { if (Clear(p, 0.6f)) Bush(g, p); }
                        else if (pick < 6) { if (Clear(p, 1f)) SmallRock(g, p, Rf(0.4f, 1.3f)); }
                        else if (pick < 7) { if (Clear(p, 0.8f)) Palm(g, p, Rf(4.5f, 7f)); }
                        else if (pick < 8) { if (Clear(p, 0.8f)) GoodsCluster(g, p); }
                    }
                }
                CombineByMaterial(g, "Dress_Route" + i, true);
                HegraKit.MarkStatic(g.gameObject);
            }
        }

        static void Horizon(Transform root)
        {
            var g = HegraKit.Group("Horizon", root, Vector3.zero).transform;
            var center = new Vector3(50f, 0, 30f);
            var rock = RockMeshes("Formation", 501, 20, 12, 0.24f, 0.2f);
            var rock2 = RockMeshes("Formation2", 733, 18, 12, 0.3f, 0.2f);

            // big sandstone formations, some carved with tomb facades on the side facing the valley
            for (int i = 0; i < 14; i++)
            {
                float a = (i / 14f) * Mathf.PI * 2f + Rf(-0.12f, 0.12f);
                float r = Rf(78f, 98f);
                var p = center + new Vector3(Mathf.Cos(a) * r * 1.15f, 0, Mathf.Sin(a) * r);
                float s = Rf(9f, 16f);
                var yaw = Quaternion.LookRotation(center - p).eulerAngles.y;
                RockObject("Formation", g, i % 2 == 0 ? rock : rock2, p, new Vector3(0, Rf(0f, 360f), 0), new Vector3(s * Rf(1.2f, 1.8f), s * Rf(1.1f, 1.6f), s), false);
                if (i % 3 == 0) TombFacade(g, p + (center - p).normalized * (s * 0.75f), yaw, Rf(0.9f, 1.3f));
            }

            // dunes so the horizon is never empty
            for (int i = 0; i < 18; i++)
            {
                float a = (i / 18f) * Mathf.PI * 2f + Rf(-0.1f, 0.1f);
                float r = Rf(118f, 150f);
                var p = center + new Vector3(Mathf.Cos(a) * r * 1.15f, -Rf(1f, 3f), Mathf.Sin(a) * r);
                var yaw = Quaternion.LookRotation(center - p).eulerAngles.y + 90f;
                HegraKit.MeshObject("Dune", g, s_LowSphere, M.sand, p, new Vector3(0, yaw, 0), new Vector3(Rf(40f, 70f), Rf(7f, 13f), Rf(18f, 28f)));
            }
            HegraKit.MarkStatic(g.gameObject);
        }

        // ───────────── props ─────────────

        static GameObject LowSphere(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null) =>
            HegraKit.MeshObject(name, parent, s_LowSphere, mat, pos, euler, scale);

        static GameObject LowCyl(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null) =>
            HegraKit.MeshObject(name, parent, s_LowCyl, mat, pos, euler, scale);

        static void Palm(Transform parent, Vector3 p, float h)
        {
            var g = HegraKit.Group("Palm", parent, p, Rf(0f, 360f)).transform;
            float lean = Rf(-6f, 6f);
            var tip = Vector3.zero;
            int segs = 6;
            for (int i = 0; i < segs; i++)
            {
                float t = (i + 0.5f) / segs;
                var c = new Vector3(Mathf.Sin(t * 1.4f) * lean * 0.04f, h * t, 0);
                float w = Mathf.Lerp(0.34f, 0.22f, t);
                LowCyl(g, "Trunk", c, new Vector3(w, h / segs * 0.52f, w), s_PalmTrunk, new Vector3(0, i * 23f, lean * t));
                tip = new Vector3(Mathf.Sin(1.4f) * lean * 0.04f, h, 0);
            }
            int fronds = Ri(8, 11);
            for (int i = 0; i < fronds; i++)
            {
                float yaw = i * 360f / fronds + Rf(-12f, 12f);
                float droop = Rf(25f, 50f);
                var rot = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(droop, 0, 0);
                var f = rot * new Vector3(0, 0, 1.05f);
                HegraKit.Prim(PrimitiveType.Cube, g, "Frond", tip + f, new Vector3(0.42f, 0.03f, 2.2f), s_PalmFrond, rot.eulerAngles);
            }
            for (int i = 0; i < 3; i++)
                LowSphere(g, "DateBunch", tip + Quaternion.Euler(0, i * 120f, 0) * new Vector3(0.24f, -0.3f, 0), new Vector3(0.22f, 0.32f, 0.22f), s_Dates);
            var col = new GameObject("PalmCollider");
            col.transform.SetParent(g, false);
            var cap = col.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0, 1.2f, 0); cap.height = 2.4f; cap.radius = 0.25f;
        }

        static void Bush(Transform parent, Vector3 p)
        {
            var g = HegraKit.Group("Bush", parent, p, Rf(0f, 360f)).transform;
            float s = Rf(0.6f, 1.2f);
            LowSphere(g, "Base", new Vector3(0, 0.1f * s, 0), new Vector3(0.7f, 0.25f, 0.6f) * s, s_DryBush);
            int n = Ri(6, 10);
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n + Rf(-15f, 15f);
                LowCyl(g, "Twig", Quaternion.Euler(0, a, 0) * new Vector3(0, 0.3f * s, 0.12f * s), new Vector3(0.025f, 0.32f * s, 0.025f), s_DryBush, new Vector3(Rf(25f, 45f), a, 0));
            }
        }

        static void SmallRock(Transform parent, Vector3 p, float s)
        {
            var rock = s_DressRnd.Next(2) == 0 ? s_Pebble : s_Pebble2;
            // the light mesh only (an LOD group per pebble would cost more than it saves once merged)
            HegraKit.MeshObject("Rock", parent, rock.low, M.rockSandstone, p, new Vector3(0, Rf(0f, 360f), 0), new Vector3(s * Rf(1f, 1.6f), s * Rf(0.6f, 1f), s));
        }

        static void CarpetWithCushions(Transform parent, Vector3 p, float yaw)
        {
            var g = HegraKit.Group("Carpet", parent, p, yaw).transform;
            var cloth = Ri(0, 2) == 0 ? M.clothRed : M.indigo;
            HegraKit.Prim(PrimitiveType.Cube, g, "Rug", new Vector3(0, 0.012f, 0), new Vector3(2.2f, 0.02f, 1.5f), cloth);
            HegraKit.Prim(PrimitiveType.Cube, g, "Border", new Vector3(0, 0.015f, 0), new Vector3(2.3f, 0.018f, 1.6f), M.clothSaffron);
            var cushions = new[] { M.clothSaffron, M.clothGreen, M.clothRed, M.linen };
            for (int i = 0; i < 3; i++)
                LowSphere(g, "Cushion", new Vector3(-0.7f + i * 0.7f, 0.1f, -0.55f), new Vector3(0.55f, 0.2f, 0.4f), cushions[Ri(0, cushions.Length)], new Vector3(0, Rf(-15f, 15f), 0));
            if (Ri(0, 2) == 0) LowCyl(g, "Tray", new Vector3(0.2f, 0.06f, 0.15f), new Vector3(0.6f, 0.02f, 0.6f), M.bronze);
        }

        static void GoodsCluster(Transform parent, Vector3 p)
        {
            var g = HegraKit.Group("Goods", parent, p, Rf(0f, 360f)).transform;
            int n = Ri(3, 6);
            for (int i = 0; i < n; i++)
            {
                var q = new Vector3(Rf(-0.9f, 0.9f), 0, Rf(-0.9f, 0.9f));
                switch (Ri(0, 6))
                {
                    case 0: Barrel(g, q); break;
                    case 1: Jar(g, q, Rf(0.8f, 1.3f)); break;
                    case 2: Basket(g, q, true); break;
                    case 3: SpiceSack(g, q); break;
                    case 4:
                        var s = new Vector3(Rf(0.5f, 0.8f), Rf(0.4f, 0.6f), Rf(0.5f, 0.8f));
                        HegraKit.Prim(PrimitiveType.Cube, g, "Crate", q + Vector3.up * s.y * 0.5f, s, M.wood, new Vector3(0, Rf(0f, 90f), 0));
                        break;
                    default: LowSphere(g, "Sack", q + Vector3.up * 0.2f, new Vector3(0.45f, 0.42f, 0.4f), M.earth); break;
                }
            }
        }

        static void Barrel(Transform g, Vector3 q)
        {
            LowCyl(g, "Barrel", q + Vector3.up * 0.4f, new Vector3(0.5f, 0.4f, 0.5f), M.wood);
            LowCyl(g, "Hoop", q + Vector3.up * 0.18f, new Vector3(0.52f, 0.03f, 0.52f), M.rope);
            LowCyl(g, "Hoop", q + Vector3.up * 0.62f, new Vector3(0.52f, 0.03f, 0.52f), M.rope);
        }

        static void Jar(Transform g, Vector3 q, float s)
        {
            LowSphere(g, "JarBelly", q + Vector3.up * 0.3f * s, new Vector3(0.36f, 0.5f, 0.36f) * s, M.pottery);
            LowCyl(g, "JarNeck", q + Vector3.up * 0.6f * s, new Vector3(0.14f, 0.1f, 0.14f) * s, M.pottery);
        }

        static void Basket(Transform g, Vector3 q, bool dates)
        {
            LowCyl(g, "Basket", q + Vector3.up * 0.14f, new Vector3(0.5f, 0.14f, 0.5f), M.rope);
            LowSphere(g, "Heap", q + Vector3.up * 0.28f, new Vector3(0.42f, 0.14f, 0.42f), dates ? s_Dates : s_SpiceBrown);
        }

        static void SpiceSack(Transform g, Vector3 q)
        {
            LowCyl(g, "Sack", q + Vector3.up * 0.25f, new Vector3(0.48f, 0.25f, 0.48f), M.linen);
            var spices = new[] { M.clothSaffron, s_SpiceRed, s_SpiceBrown };
            LowSphere(g, "Spice", q + Vector3.up * 0.5f, new Vector3(0.44f, 0.18f, 0.44f), spices[Ri(0, spices.Length)]);
        }

        static void StallGoods(Transform stall, int variant)
        {
            // on the counter (local z 0.6, top at 0.95) and on the shelf (z -0.55, top at 1.33)
            for (int i = 0; i < 5; i++)
            {
                float x = -0.9f + i * 0.45f;
                var top = new Vector3(x, 0.95f, 0.62f);
                switch ((i + variant) % 4)
                {
                    case 0: Basket(stall, top, true); break;
                    case 1: Jar(stall, top, 0.55f); break;
                    case 2: LowSphere(stall, "Spice", top + Vector3.up * 0.06f, new Vector3(0.3f, 0.13f, 0.3f), new[] { M.clothSaffron, s_SpiceRed, s_SpiceBrown }[variant % 3]); break;
                    default: HegraKit.Prim(PrimitiveType.Cube, stall, "Cloth", top + Vector3.up * 0.04f, new Vector3(0.36f, 0.08f, 0.28f), new[] { M.clothRed, M.indigo, M.clothGreen }[variant % 3]); break;
                }
                Jar(stall, new Vector3(x, 1.33f, -0.55f), 0.4f);
            }
            SpiceSack(stall, new Vector3(-1.5f, 0, 1.2f));
            Basket(stall, new Vector3(1.5f, 0, 1.2f), variant % 2 == 0);
        }

        static void FabricLine(Transform parent, Vector3 a, Vector3 b)
        {
            var g = HegraKit.Group("FabricLine", parent, Vector3.zero).transform;
            foreach (var p in new[] { a, b }) LowCyl(g, "Pole", p + Vector3.up * 1.2f, new Vector3(0.07f, 1.2f, 0.07f), M.wood);
            var mid = (a + b) * 0.5f + Vector3.up * 2.25f;
            var dir = b - a;
            HegraKit.Prim(PrimitiveType.Cube, g, "Rope", mid, new Vector3(0.02f, 0.02f, dir.magnitude), M.rope, new Vector3(0, Quaternion.LookRotation(dir).eulerAngles.y, 0));
            var cloths = new[] { M.clothRed, M.clothSaffron, M.indigo, M.clothGreen, M.violet };
            int n = Mathf.FloorToInt(dir.magnitude / 1.1f);
            for (int i = 0; i < n; i++)
            {
                var p = Vector3.Lerp(a, b, (i + 0.5f) / n) + Vector3.up * Rf(1.55f, 1.75f);
                HegraKit.Prim(PrimitiveType.Cube, g, "Fabric", p, new Vector3(0.03f, Rf(0.9f, 1.3f), 0.8f), cloths[i % cloths.Length], new Vector3(0, Quaternion.LookRotation(dir).eulerAngles.y, Rf(-4f, 4f)));
            }
        }

        static void IncenseBurner(Transform parent, Vector3 p)
        {
            var g = HegraKit.Group("Incense", parent, p).transform;
            LowCyl(g, "Stand", new Vector3(0, 0.4f, 0), new Vector3(0.08f, 0.4f, 0.08f), M.bronze);
            LowCyl(g, "Bowl", new Vector3(0, 0.84f, 0), new Vector3(0.3f, 0.05f, 0.3f), M.bronze);
            LowSphere(g, "Coals", new Vector3(0, 0.88f, 0), new Vector3(0.22f, 0.06f, 0.22f), M.ember);
            var glow = HegraKit.Prim(PrimitiveType.Quad, g, "Glow", new Vector3(0, 0.98f, 0), Vector3.one * 0.45f, M.glowFire);
            var fl = glow.AddComponent<FlameFlicker>(); fl.billboardYaw = true; fl.amount = 0.15f;
            var smoke = NewParticles("Smoke", g, new Vector3(0, 0.95f, 0), HegraKit.GlowParticle("M_IncenseSmoke", new Color(0.55f, 0.5f, 0.45f), 0.35f, 1.6f), 18);
            var main = smoke.main;
            main.playOnAwake = true; main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            var em = smoke.emission; em.rateOverTime = 4f;
            var sh = smoke.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.04f; sh.rotation = new Vector3(-90f, 0, 0);
            var col = smoke.colorOverLifetime; col.enabled = true; col.color = FadeInOut(Color.white);
            var size = smoke.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2.2f));
            var noise = smoke.noise; noise.enabled = true; noise.strength = 0.12f; noise.frequency = 0.5f;
        }

        static void Well(Transform parent, Vector3 p)
        {
            var g = HegraKit.Group("Well", parent, p).transform;
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                HegraKit.Prim(PrimitiveType.Cube, g, "WellStone", new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)) * 0.9f + Vector3.up * 0.0f,
                    new Vector3(0.55f, 0.6f, 0.3f), M.sandstoneShadow, new Vector3(0, -i * 36f + 90f, 0));
            }
            LowCyl(g, "Water", new Vector3(0, 0.45f, 0), new Vector3(1.5f, 0.02f, 1.5f), M.water);
            foreach (var x in new[] { -1.1f, 1.1f }) LowCyl(g, "Post", new Vector3(x, 1f, 0), new Vector3(0.08f, 1f, 0.08f), M.wood);
            HegraKit.Prim(PrimitiveType.Cube, g, "Beam", new Vector3(0, 1.95f, 0), new Vector3(2.4f, 0.1f, 0.1f), M.wood);
            LowCyl(g, "Bucket", new Vector3(0.3f, 1.3f, 0), new Vector3(0.22f, 0.14f, 0.22f), M.wood);
            var col = new GameObject("WellCollider");
            col.transform.SetParent(g, false);
            var c = col.AddComponent<CapsuleCollider>(); c.radius = 1.1f; c.height = 2f; c.center = Vector3.up;
        }

        /// <summary>A Nabataean tomb facade carved into a rock face: crow-step crown, pilasters, cornice and a dark doorway.</summary>
        static void TombFacade(Transform parent, Vector3 p, float yaw, float s)
        {
            var g = HegraKit.Group("TombFacade", parent, p, yaw).transform;
            g.localScale = Vector3.one * s;
            var stone = M.sandstoneLit;
            HegraKit.Prim(PrimitiveType.Cube, g, "Face", new Vector3(0, 5.5f, 0), new Vector3(8f, 11f, 1.2f), stone);
            HegraKit.Prim(PrimitiveType.Cube, g, "Cornice", new Vector3(0, 8.4f, 0.75f), new Vector3(8.6f, 0.5f, 0.4f), stone);
            HegraKit.Prim(PrimitiveType.Cube, g, "Frieze", new Vector3(0, 7.9f, 0.68f), new Vector3(8.2f, 0.35f, 0.25f), stone);
            foreach (var x in new[] { -3.6f, 3.6f })
                HegraKit.Prim(PrimitiveType.Cube, g, "Pilaster", new Vector3(x, 4f, 0.7f), new Vector3(0.6f, 7.8f, 0.2f), stone);
            HegraKit.Prim(PrimitiveType.Cube, g, "Doorway", new Vector3(0, 1.7f, 0.62f), new Vector3(1.8f, 3.4f, 0.1f), M.darkInterior);
            HegraKit.Prim(PrimitiveType.Cube, g, "DoorFrame", new Vector3(0, 3.6f, 0.68f), new Vector3(2.6f, 0.35f, 0.2f), stone);
            HegraKit.Prim(PrimitiveType.Cube, g, "Pediment", new Vector3(0, 4.1f, 0.7f), new Vector3(2.2f, 0.5f, 0.15f), stone);
            CrowSteps(g, new Vector3(0, 8.65f, 0.55f), 8f, 0.6f, stone, 4);
        }

        // ───────────── life ─────────────

        static void SittingCircle(Transform life, Vector3 fire, int count, int variant)
        {
            var robes = new[] { M.earth, M.indigo, M.clothGreen, M.mantleRed, M.linen };
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + 0.5f + variant;
                var p = fire + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.5f;
                var yaw = Quaternion.LookRotation(fire - p).eulerAngles.y;
                var f = HegraCharacters.Build("SittingPerson", life, p, yaw, robes[(i + variant) % robes.Length], i % 2 == 0 ? M.skinA : M.skinB, 1.7f);
                if (i % 2 == 0) HegraCharacters.Headcloth(f, M.linen); else HegraCharacters.Veil(f, M.clothSaffron);
                // seated: sink the figure so only the upper body shows above a cushion
                f.root.transform.position += Vector3.down * 0.55f;
                Object.DestroyImmediate(f.root.GetComponent<CapsuleCollider>());
                LowSphere(f.root.transform.parent, "SeatCushion", p + Vector3.up * 0.08f, new Vector3(0.75f, 0.2f, 0.65f), M.clothRed, new Vector3(0, yaw, 0));
                f.npc.facePlayer = false;
                f.npc.idleGesture = i == 0 ? NpcGesture.Talk : NpcGesture.Idle;
            }
        }

        static void Child(Transform life, Vector3 center, float radius)
        {
            var c = HegraCharacters.Build("Child", life, center + new Vector3(radius, 0, 0), 0f, M.clothSaffron, M.skinA, 1.12f);
            Object.DestroyImmediate(c.root.GetComponent<CapsuleCollider>());
            var w = c.root.AddComponent<NpcWalker>();
            var pts = new Vector3[8];
            for (int i = 0; i < pts.Length; i++)
            {
                float a = i * Mathf.PI * 2f / pts.Length;
                pts[i] = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
            }
            w.points = pts; w.loop = true; w.speed = 1.6f; w.pause = 0.2f; w.bob = 0.07f;
            c.npc.idleGesture = NpcGesture.Wave;
        }

        static void Walker(Transform life, Vector3[] points, Material robe, Material skin, float speed)
        {
            var f = HegraCharacters.Build("Stroller", life, points[0], 0f, robe, skin, Rf(1.62f, 1.8f));
            if (Ri(0, 2) == 0) HegraCharacters.Headcloth(f, M.linen); else HegraCharacters.Veil(f, M.clothSaffron);
            Object.DestroyImmediate(f.root.GetComponent<CapsuleCollider>());
            var w = f.root.AddComponent<NpcWalker>();
            w.points = points; w.speed = speed; w.pause = 3f;
        }

        static void SittingCamel(Transform life, Vector3 p, float yaw)
        {
            var c = HegraCharacters.Camel(life, p, yaw, M);
            c.name = "CamelResting";
            for (int i = 0; i < 4; i++) { var leg = c.transform.Find("Leg" + i); if (leg) Object.DestroyImmediate(leg.gameObject); }
            c.transform.position += Vector3.down * 0.85f;
            var box = c.GetComponent<BoxCollider>();
            if (box) { box.center = new Vector3(0, 1.4f, 0.2f); box.size = new Vector3(0.9f, 1.4f, 2.4f); }
        }

        // ───────────── scatter ─────────────

        static void Scatter(Transform parent, Vector3 center, float rMin, float rMax, int count, bool palms)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Rf(0f, Mathf.PI * 2f), r = Rf(rMin, rMax);
                var p = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                if (!Clear(p, 0.8f)) continue;
                int pick = Ri(0, palms ? 6 : 5);
                if (pick < 3) Bush(parent, p);
                else if (pick < 5) SmallRock(parent, p, Rf(0.3f, 1f));
                else Palm(parent, p, Rf(4.5f, 7f));
            }
        }

        // ───────────── low-poly meshes ─────────────

        static Mesh LowSphereMesh()
        {
            // icosphere, one subdivision (80 triangles) — enough for small props, far lighter than Unity's sphere
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized * 0.5f;
            var f = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            var mid = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (mid.TryGetValue(key, out var i)) return i;
                v.Add(((v[a] + v[b]) * 0.5f).normalized * 0.5f);
                mid[key] = v.Count - 1;
                return v.Count - 1;
            }
            var f2 = new List<int>();
            for (int i = 0; i < f.Count; i += 3)
            {
                int a = f[i], b = f[i + 1], c = f[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                f2.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetTriangles(f2, 0);
            var uv = new Vector2[v.Count];
            for (int i = 0; i < v.Count; i++) uv[i] = new Vector2(v[i].x + 0.5f, v[i].y + 0.5f);
            m.uv = uv;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return HegraKit.SaveMesh(m, "LowSphere");
        }

        static Mesh LowCylinderMesh(int sides)
        {
            // matches Unity's cylinder proportions: radius 0.5, from y -1 to 1
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                var d = new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f);
                v.Add(d + Vector3.down); v.Add(d + Vector3.up);
            }
            for (int i = 0; i < sides; i++)
            {
                int a = i * 2;
                t.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
            }
            foreach (var y in new[] { -1f, 1f })
            {
                int c = v.Count;
                v.Add(new Vector3(0, y, 0));
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    v.Add(new Vector3(Mathf.Cos(a) * 0.5f, y, Mathf.Sin(a) * 0.5f));
                }
                for (int i = 0; i < sides; i++)
                {
                    int p = c + 1 + i, q = c + 1 + (i + 1) % sides;
                    t.AddRange(y > 0 ? new[] { c, q, p } : new[] { c, p, q });
                }
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            var uv = new Vector2[v.Count];
            for (int i = 0; i < v.Count; i++) uv[i] = new Vector2(Mathf.Atan2(v[i].z, v[i].x) / 6.283f + 0.5f, v[i].y * 0.5f + 0.5f);
            m.uv = uv;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return HegraKit.SaveMesh(m, "LowCylinder");
        }
    }
}
