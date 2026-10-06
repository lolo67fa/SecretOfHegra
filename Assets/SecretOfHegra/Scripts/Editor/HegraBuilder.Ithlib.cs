using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra.EditorTools
{
    public static partial class HegraBuilder
    {
        const float Cell = 1.4f;

        static Vector3 CellPos(int x, int y) => new Vector3((x - 1) * Cell, 0, 2.6f - y * Cell);

        static Vector3 DirVec(int d) => d == WaterNetwork.N ? Vector3.forward : d == WaterNetwork.S ? Vector3.back : d == WaterNetwork.E ? Vector3.right : Vector3.left;

        /// <summary>Grooves + water strips for each port of a channel piece (under <paramref name="parent"/>).</summary>
        static IthlibStop.CellView ChannelPiece(Transform parent, int x, int y, int mask, Material water)
        {
            var v = new IthlibStop.CellView { x = x, y = y };
            foreach (var d in new[] { WaterNetwork.N, WaterNetwork.E, WaterNetwork.S, WaterNetwork.W })
            {
                if ((mask & d) == 0) continue;
                var dir = DirVec(d);
                var side = Vector3.Cross(Vector3.up, dir);
                float len = Cell * 0.5f;
                var mid = dir * (len * 0.5f);
                var rot = Quaternion.LookRotation(dir).eulerAngles;
                HegraKit.Prim(PrimitiveType.Cube, parent, "Groove", mid + side * 0.2f + Vector3.up * 0.16f, new Vector3(0.1f, 0.14f, len), M.sandstoneShadow, rot);
                HegraKit.Prim(PrimitiveType.Cube, parent, "Groove", mid - side * 0.2f + Vector3.up * 0.16f, new Vector3(0.1f, 0.14f, len), M.sandstoneShadow, rot);
                var strip = HegraKit.Prim(PrimitiveType.Cube, parent, "Water_" + d, mid + Vector3.up * 0.125f, new Vector3(0.3f, 0.02f, len), water, rot);
                if (d == WaterNetwork.N) v.waterN = strip; else if (d == WaterNetwork.E) v.waterE = strip; else if (d == WaterNetwork.S) v.waterS = strip; else v.waterW = strip;
            }
            v.pool = HegraKit.Prim(PrimitiveType.Cube, parent, "Pool", Vector3.up * 0.126f, new Vector3(0.42f, 0.02f, 0.42f), water);
            return v;
        }

        static IthlibStop BuildIthlib(Transform world, HegraAudioAssets.Set audio)
        {
            float yaw = YawTo(TheaterPos, IthlibPos);
            var root = HegraKit.Group("Stop4_JabalIthlib", world, IthlibPos, yaw).transform;
            var stop = root.gameObject.AddComponent<IthlibStop>();
            stop.stopId = StopId.Ithlib;
            stop.spawnPoint = Point(root, "SpawnPoint", new Vector3(0, 0, -6.5f), new Vector3(0, 0, 2f));
            stop.guidePoint = Point(root, "GuidePoint", new Vector3(-3.2f, 0, -2.6f), new Vector3(0, 0, -6f));
            stop.puzzleCenter = Point(root, "PuzzleCenter", new Vector3(0, 0.5f, 0.5f), Vector3.zero);
            var water = HegraKit.Custom("M_WaterFlow", "SecretOfHegra/WaterFlow");

            // rock massif and a carved "Diwan"-like hall facade (decor)
            var decor = HegraKit.Group("Decor", root, Vector3.zero).transform;
            var big = RockMeshes("IthlibMassif", 13, 20, 12, 0.25f, 0.2f);
            RockObject("Massif", decor, big, new Vector3(-1f, 0, 11f), new Vector3(0, 20, 0), new Vector3(11f, 7f, 5f), true);
            RockObject("MassifW", decor, big, new Vector3(-10f, 0, 6f), new Vector3(0, 80, 0), new Vector3(6f, 5f, 4f), true);
            var diwan = HegraKit.Group("DiwanFacade", decor, new Vector3(-7.2f, 0, 3.6f), 60f).transform;
            HegraKit.Prim(PrimitiveType.Cube, diwan, "Face", new Vector3(0, 2f, 0), new Vector3(4.4f, 4f, 0.4f), M.sandstoneLit, null, true);
            HegraKit.Prim(PrimitiveType.Cube, diwan, "Opening", new Vector3(0, 1.3f, -0.21f), new Vector3(2.2f, 2.6f, 0.02f), HegraKit.Unlit("M_Dark", new Color(0.03f, 0.02f, 0.03f)));
            HegraKit.Prim(PrimitiveType.Cube, diwan, "Lintel", new Vector3(0, 2.75f, -0.25f), new Vector3(2.6f, 0.2f, 0.1f), M.sandstoneShadow);

            // spring at the north edge, always flowing into the first cell
            var spring = HegraKit.Group("Spring", root, CellPos(1, 0) + new Vector3(0, 0, 1.3f)).transform;
            HegraKit.MeshObject("SpringRock", spring, HegraKit.Rock("SpringRock", 5, 12, 8, 0.2f, 0.4f), M.rockSandstone, new Vector3(0, 0.3f, 0.6f), null, new Vector3(1.0f, 0.8f, 0.8f), true);
            HegraKit.Prim(PrimitiveType.Cube, spring, "SpringWater", new Vector3(0, 0.13f, -0.25f), new Vector3(0.3f, 0.02f, 0.9f), water);
            Loop3D(spring, "SpringSound", audio.Get("amb_water"), audio.sfxGroup, 0.5f, 9f);

            // the channel network
            var net = WaterNetwork.CreateIthlib();
            var grid = HegraKit.Group("Channels", root, Vector3.zero).transform;
            var views = new List<IthlibStop.CellView>();
            var gatesById = new Dictionary<string, ChannelGate>();
            for (int x = 0; x < net.width; x++)
                for (int y = 0; y < net.height; y++)
                {
                    var c = net[x, y];
                    var cellRoot = HegraKit.Group($"Cell_{x}_{y}_{c?.id}", grid, CellPos(x, y)).transform;
                    HegraKit.Prim(PrimitiveType.Cube, cellRoot, "Bed", new Vector3(0, 0.05f, 0), new Vector3(Cell * 0.96f, 0.1f, Cell * 0.96f), M.sandstoneLit);
                    if (c == null) continue;
                    if (c.cistern)
                    {
                        // cistern: low stone walls with water that rises when solved
                        foreach (var d in new[] { Vector3.left, Vector3.right, Vector3.back })
                            HegraKit.Prim(PrimitiveType.Cube, cellRoot, "CisternWall", d * 0.62f + Vector3.up * 0.3f, d.x != 0 ? new Vector3(0.16f, 0.6f, 1.4f) : new Vector3(1.4f, 0.6f, 0.16f), M.sandstoneShadow, null, true);
                        var v = ChannelPiece(cellRoot, x, y, WaterNetwork.N, water);
                        views.Add(v);
                        var w = HegraKit.Prim(PrimitiveType.Cube, cellRoot, "CisternWater", new Vector3(0, 0.12f, -0.05f), new Vector3(1.05f, 0.04f, 1.05f), water);
                        stop.cisternWater = w.transform;
                        stop.cisternLow = Point(cellRoot, "CisternLow", new Vector3(0, 0.12f, -0.05f), Vector3.zero);
                        stop.cisternHigh = Point(cellRoot, "CisternHigh", new Vector3(0, 0.5f, -0.05f), Vector3.zero);
                        continue;
                    }
                    if (!c.rotatable)
                    {
                        views.Add(ChannelPiece(cellRoot, x, y, c.baseMask, water));
                        if (c.id == "SpillBasin")
                            HegraKit.Prim(PrimitiveType.Cylinder, cellRoot, "DryBasin", new Vector3(0, 0.12f, 0), new Vector3(0.9f, 0.02f, 0.9f), M.rough);
                        continue;
                    }
                    // rotatable gate: disc + grooves on a rotor, shaft and T-handle at waist height
                    var gateGo = cellRoot.gameObject;
                    var rotor = HegraKit.Group("Rotor", cellRoot, Vector3.zero).transform;
                    HegraKit.Prim(PrimitiveType.Cylinder, rotor, "Disc", new Vector3(0, 0.1f, 0), new Vector3(1.2f, 0.04f, 1.2f), M.sandstoneLit);
                    views.Add(ChannelPiece(rotor, x, y, c.baseMask, water));
                    HegraKit.Prim(PrimitiveType.Cylinder, rotor, "Shaft", new Vector3(0, 0.55f, 0), new Vector3(0.09f, 0.45f, 0.09f), M.wood, null, true);
                    var handle = HegraKit.Prim(PrimitiveType.Cube, rotor, "Handle", new Vector3(0, 0.98f, 0), new Vector3(0.12f, 0.08f, 0.62f), M.wood, null, true);
                    HegraKit.Prim(PrimitiveType.Cube, rotor, "HandleCloth", new Vector3(0, 0.98f, 0.28f), new Vector3(0.14f, 0.1f, 0.08f), M.clothRed);
                    // arrow on the disc shows where the water goes
                    var glow = HegraKit.Prim(PrimitiveType.Quad, rotor, "CorrectGlow", new Vector3(0, 0.125f, 0), new Vector3(1.3f, 1.3f, 1f), M.glowBlue, new Vector3(90, 0, 0));
                    var gi = gateGo.AddComponent<XRSimpleInteractable>();
                    var gate = gateGo.AddComponent<ChannelGate>();
                    gate.rotor = rotor;
                    gate.correctGlow = glow.GetComponent<Renderer>();
                    gate.correctGlow.enabled = false;
                    gate.quarterTurns = c.rotation;
                    gatesById[c.id] = gate;
                }
            stop.cells = views;
            stop.gates = new List<ChannelGate> { gatesById["GateA"], gatesById["GateB"], gatesById["GateC"] };

            // splash where the water currently ends (unsolved)
            var spill = HegraKit.Prim(PrimitiveType.Quad, root, "SpillSplash", Vector3.up * 0.2f, Vector3.one * 0.8f, M.glowBlue);
            spill.AddComponent<FlameFlicker>().billboardYaw = true;
            stop.spillMarker = spill.transform;
            stop.spillSound = Loop3D(spill.transform, "SpillSound", audio.Get("amb_water"), audio.sfxGroup, 0.6f, 7f);

            // reset lever beside the viewing spot
            var lever = HegraKit.Group("ResetLever", root, new Vector3(-2.8f, 0, -2.0f)).transform;
            HegraKit.Prim(PrimitiveType.Cylinder, lever, "Post", new Vector3(0, 0.45f, 0), new Vector3(0.1f, 0.45f, 0.1f), M.sandstoneShadow);
            HegraKit.Prim(PrimitiveType.Cube, lever, "Lever", new Vector3(0, 1.0f, 0.05f), new Vector3(0.08f, 0.25f, 0.08f), M.wood, new Vector3(20, 0, 0), true);
            HegraKit.Prim(PrimitiveType.Sphere, lever, "Knob", new Vector3(0, 1.13f, 0.1f), Vector3.one * 0.1f, M.clothRed, null, true);
            var lt = WorldText(lever, "Label", new Vector3(0, 1.35f, 0), 1.2f, "menu.reset_puzzle", HegraPalette.Linen, 0.8f);
            stop.resetLever = lever.gameObject.AddComponent<XRSimpleInteractable>();

            // narrow passage (siq) toward Qasr al-Bint, blocked by a slab until the cistern fills
            var toBint = (BintPos - IthlibPos); toBint.y = 0; toBint.Normalize();
            var perp = Vector3.Cross(Vector3.up, toBint);
            var siqWorld = IthlibPos + toBint * 8f;
            var siqLocal = root.InverseTransformPoint(siqWorld);
            var siqYaw = Quaternion.LookRotation(root.InverseTransformDirection(toBint)).eulerAngles.y;
            var siqRock = RockMeshes("SiqRock", 31, 16, 10, 0.2f, 0.2f);
            RockObject("SiqWallL", root, siqRock, root.InverseTransformPoint(siqWorld + perp * 3.3f), new Vector3(0, siqYaw, 0), new Vector3(2.4f, 5f, 4.5f), true);
            RockObject("SiqWallR", root, siqRock, root.InverseTransformPoint(siqWorld - perp * 3.3f), new Vector3(0, siqYaw + 180, 0), new Vector3(2.4f, 5f, 4.5f), true);
            var slab = HegraKit.Prim(PrimitiveType.Cube, root, "PassageSlab", siqLocal + Vector3.up * 1.3f, new Vector3(2.8f, 2.6f, 0.5f), M.sandstoneLit, new Vector3(0, siqYaw, 0), true);
            stop.passageSlab = slab.transform;
            stop.passageOpenOffset = root.InverseTransformDirection(perp * 2.6f);
            stop.toBint = Point(root, "ToBint", root.InverseTransformPoint(siqWorld + toBint * 3f) + Vector3.up * 1.5f, Vector3.zero);

            Lantern(root, new Vector3(-2.4f, 0, -3.6f));
            Lantern(root, new Vector3(2.4f, 0, -3.6f));
            Lantern(root, new Vector3(3.2f, 0, 3.2f), 1.8f);
            HegraKit.MarkStatic(decor.gameObject);

            var zone = root.gameObject.AddComponent<AmbienceZone>();
            zone.stop = StopId.Ithlib;
            zone.radius = 13f;
            zone.ambience = audio.Get("amb_wind");
            zone.ambienceVolume = 0.2f;

            EduCardObject(root, new Vector3(3.6f, 0, -4.2f), "edu.ithlib");
            return stop;
        }
    }
}
