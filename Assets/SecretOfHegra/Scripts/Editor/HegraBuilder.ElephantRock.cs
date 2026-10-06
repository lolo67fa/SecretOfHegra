using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra.EditorTools
{
    public static partial class HegraBuilder
    {
        /// <summary>Glyphs on the rock face (left to right) and the three the player must repeat, in order (Q-M-R, "moon").</summary>
        public static readonly string[] ElephantGlyphLetters = { "ن", "ق", "ح", "ر", "م", "ل" };
        public static readonly int[] ElephantGlyphOrder = { 1, 4, 3 };

        /// <summary>Theater-local placement: beyond stage right in open sand, seen from the seats, beside the way on to Jabal Ithlib.</summary>
        static readonly Vector3 k_ElephantPos = new Vector3(17f, 0f, 13f);

        // ───────────── Elephant Rock (inspired by Jabal AlFil, AlUla) ─────────────

        /// <summary>
        /// A lightweight sandstone formation shaped like AlUla's Elephant Rock: a large rounded body, a lower head,
        /// a trunk-like column descending to the ground and an open gap between trunk and body. Seen in profile
        /// from the theater. Its smoothed face carries the fictional, Nabataean-inspired inscription for Sign 1.
        /// Rock-local axes: +X = toward the head, -Z = the face turned to the theater.
        /// </summary>
        static ElephantRockPuzzle BuildElephantRock(Transform theater, Vector3 theaterCenter)
        {
            var toTheater = theaterCenter - k_ElephantPos; toTheater.y = 0;
            float yaw = Quaternion.LookRotation(-toTheater).eulerAngles.y; // local -Z faces the seats
            var root = HegraKit.Group("ElephantRock", theater, k_ElephantPos, yaw).transform;
            var forms = HegraKit.Group("Formation", root, Vector3.zero).transform;

            // body: the high rounded back; head: lower, projecting forward; both share the sandstone shader
            var bodyMeshes = HegraKit.RoundRock("ElephantBody", 71, 0.7f, 0.14f, 0.3f);
            var body = RockObject("Body", forms, bodyMeshes, new Vector3(-1.8f, 1.3f, 0f), new Vector3(0, 3f, 0), new Vector3(4.8f, 4.9f, 2.6f), true);
            // the head grows out of the body's front shoulder (no sky between them) and droops toward the trunk
            RockObject("Head", forms, HegraKit.RoundRock("ElephantHead", 83, 0.85f, 0.12f, 0.8f), new Vector3(3.4f, 4.0f, 0f), new Vector3(0, -4f, -12f), new Vector3(2.1f, 1.75f, 1.75f), true);

            // trunk: emerges under the head's front and descends to the sand, with a flared foot
            var path = new[]
            {
                new Vector3(4.9f, 3.85f, 0f), new Vector3(5.55f, 3.35f, 0f), new Vector3(5.95f, 2.8f, 0f), new Vector3(6.15f, 2.15f, 0f),
                new Vector3(6.2f, 1.5f, 0f), new Vector3(6.15f, 0.9f, 0f), new Vector3(6.1f, 0.4f, 0f), new Vector3(6.15f, -0.15f, 0f)
            };
            var radii = new[] { 1.0f, 0.92f, 0.8f, 0.7f, 0.64f, 0.64f, 0.74f, 0.92f };
            var trunkMesh = HegraKit.Column("ElephantTrunk", 97, path, radii, 14, 0.85f, 0.12f);
            HegraKit.MeshObject("Trunk", forms, trunkMesh, M.rockSandstone, Vector3.zero, null, null, true);

            // weathered rubble at the foot
            var rubble = RockMeshes("ElephantRubble", 59, 12, 8, 0.25f, 0.3f);
            RockObject("Rubble1", forms, rubble, new Vector3(-6.4f, 0.15f, -1.5f), new Vector3(0, 40, 0), new Vector3(0.7f, 0.5f, 0.6f), false);
            RockObject("Rubble2", forms, rubble, new Vector3(1.6f, 0.1f, -2.9f), new Vector3(0, 130, 0), new Vector3(0.45f, 0.35f, 0.4f), false);
            RockObject("Rubble3", forms, rubble, new Vector3(7.2f, 0.1f, 0.9f), new Vector3(0, 210, 0), new Vector3(0.5f, 0.4f, 0.45f), false);
            HegraKit.MarkStatic(forms.gameObject);

            // inscription face: a smoothed panel set into the body's flank, just proud of the most protruding stone
            var panelCenter = new Vector3(-1.6f, 1.3f, 0f);
            float surfaceZ = FrontSurfaceZ(body.transform, bodyMeshes.high, root, panelCenter.x - 1.4f, panelCenter.x + 1.4f, 0.55f, 2.1f);
            var faceRoot = HegraKit.Group("InscriptionFace", root, new Vector3(panelCenter.x, panelCenter.y, surfaceZ - 0.04f)).transform;
            var slab = HegraKit.Prim(PrimitiveType.Cube, faceRoot, "Slab", new Vector3(0, 0, 0.36f), new Vector3(2.7f, 1.25f, 0.72f), M.sandstoneLit, new Vector3(0, 0, -1.5f), true);
            HegraKit.Prim(PrimitiveType.Cube, faceRoot, "Lintel", new Vector3(0.1f, 0.68f, 0.32f), new Vector3(2.9f, 0.14f, 0.7f), M.sandstoneShadow, new Vector3(0, 0, 1f));

            var puzzle = root.gameObject.AddComponent<ElephantRockPuzzle>();
            puzzle.face = faceRoot;
            puzzle.order = (int[])ElephantGlyphOrder.Clone();
            var carvings = new List<TMP_Text>();
            float spacing = 0.4f;
            for (int i = 0; i < ElephantGlyphLetters.Length; i++)
            {
                // laid out right-to-left like the script it imitates
                float x = ((ElephantGlyphLetters.Length - 1) * 0.5f - i) * spacing;
                var tile = HegraKit.Prim(PrimitiveType.Cube, faceRoot, "Glyph" + i, new Vector3(x, -0.02f, 0.012f), new Vector3(0.32f, 0.42f, 0.03f), M.sandstoneShadow, null, true);
                var box = tile.GetComponent<BoxCollider>();
                box.size = new Vector3(1.15f, 1.1f, 3f); // generous ray target (local to the thin tile)
                var glyphGlow = HegraKit.Prim(PrimitiveType.Quad, faceRoot, "GlyphGlow" + i, new Vector3(x, -0.02f, -0.006f), new Vector3(0.6f, 0.66f, 1f), M.glowWarm);
                var t = CarvedText(faceRoot, "Letter" + i, new Vector3(x, -0.02f, -0.008f), NabataeanDisplay(ElephantGlyphLetters[i]), 3.2f, puzzle.carvedColor, s_NabFont);
                t.rectTransform.sizeDelta = new Vector2(0.34f, 0.42f);
                carvings.Add(t);
                puzzle.glyphs.Add(t);
                puzzle.glyphGlows.Add(glyphGlow.GetComponent<Renderer>());
                puzzle.glyphButtons.Add(tile.AddComponent<XRSimpleInteractable>());
            }

            var revealGlow = HegraKit.Prim(PrimitiveType.Quad, faceRoot, "RevealGlow", new Vector3(0, 0, -0.004f), new Vector3(3.0f, 1.5f, 1f), M.glowWarm);
            var reveal = faceRoot.gameObject.AddComponent<LampRevealTarget>();
            reveal.carvings = carvings.ToArray();
            reveal.glow = revealGlow.GetComponent<Renderer>();
            reveal.wall = slab.GetComponent<Collider>();
            reveal.ignoreUnder = root;
            reveal.range = 1.5f;
            puzzle.reveal = reveal;

            // world response when solved: the face warms and gold light fills the gap beneath the head
            puzzle.faceGlow = HegraKit.Prim(PrimitiveType.Quad, faceRoot, "SolvedGlow", new Vector3(0, 0, -0.01f), new Vector3(3.8f, 2.2f, 1f), M.glowWarm).GetComponent<Renderer>();
            var gap = HegraKit.Prim(PrimitiveType.Quad, root, "GapGlow", new Vector3(4.4f, 1.4f, -0.3f), new Vector3(2.6f, 2.9f, 1f), M.glowWarm);
            puzzle.gapGlow = gap.GetComponent<Renderer>();
            // solved-state lights start off (the puzzle also switches them at runtime)
            puzzle.faceGlow.enabled = false;
            puzzle.gapGlow.enabled = false;
            revealGlow.GetComponent<Renderer>().enabled = false;
            foreach (var g in puzzle.glyphGlows) g.enabled = false;

            // where to stand: a soft pool of light in front of the face, plus a lantern to the side
            HegraKit.MeshObject("StandHere", root, HegraKit.GroundQuad(), M.glowPool, new Vector3(panelCenter.x, 0.02f, surfaceZ - 1.4f), null, new Vector3(1.4f, 1f, 1.4f));
            Lantern(root, new Vector3(panelCenter.x + 2.3f, 0, surfaceZ - 1.6f), 1.9f);
            EduCardObject(root, new Vector3(panelCenter.x - 3.4f, 0, surfaceZ - 2.4f), "edu.elephant");
            return puzzle;
        }

        /// <summary>Most protruding (minimum) rock-local Z of the mesh within an x/y window: the panel goes just in front.</summary>
        static float FrontSurfaceZ(Transform meshTransform, Mesh mesh, Transform rockRoot, float xMin, float xMax, float yMin, float yMax)
        {
            float best = 0f;
            bool any = false;
            foreach (var v in mesh.vertices)
            {
                var p = rockRoot.InverseTransformPoint(meshTransform.TransformPoint(v));
                if (p.x < xMin || p.x > xMax || p.y < yMin || p.y > yMax || p.z > 0f) continue;
                if (!any || p.z < best) { best = p.z; any = true; }
            }
            return any ? best : -2.6f;
        }
    }
}
