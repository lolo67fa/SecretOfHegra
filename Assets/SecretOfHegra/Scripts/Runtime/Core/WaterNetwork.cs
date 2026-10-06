using System.Collections.Generic;

namespace SecretOfHegra
{
    /// <summary>
    /// Deterministic channel-routing puzzle for Jabal Ithlib. A grid of channel pieces; each piece has ports
    /// (N/E/S/W bitmask) and some are rotatable gates (90° steps, clockwise). Water enters at the spring
    /// and follows connected ports; the puzzle is solved when it reaches the cistern. No fluid simulation.
    /// </summary>
    public class WaterNetwork
    {
        public const int N = 1, E = 2, S = 4, W = 8;

        public class Cell
        {
            public int baseMask;
            public bool rotatable;
            public int rotation;          // 0..3 clockwise quarter turns
            public int solvedRotation = -1;
            public bool cistern;
            public string id;
            public int Mask => Rotate(baseMask, rotation);
        }

        public readonly int width, height;
        readonly Cell[,] m_Cells;
        public int sourceX, sourceY, sourceEntry = N;

        public WaterNetwork(int w, int h)
        {
            width = w; height = h;
            m_Cells = new Cell[w, h];
        }

        public Cell this[int x, int y] { get => m_Cells[x, y]; set => m_Cells[x, y] = value; }

        public static int Rotate(int mask, int quarterTurns)
        {
            quarterTurns = ((quarterTurns % 4) + 4) % 4;
            for (int i = 0; i < quarterTurns; i++)
                mask = ((mask << 1) | (mask >> 3)) & 0xF; // N→E→S→W→N
            return mask;
        }

        static int Opposite(int d) => d == N ? S : d == S ? N : d == E ? W : E;

        public struct FlowResult
        {
            public bool solved;
            public HashSet<(int x, int y)> wet;
            public Dictionary<(int x, int y), int> wetPorts;   // ports carrying water per cell
            public (int x, int y) stopCell;                     // where the water ends when unsolved
        }

        public FlowResult Flow()
        {
            var r = new FlowResult { wet = new HashSet<(int, int)>(), wetPorts = new Dictionary<(int, int), int>(), stopCell = (sourceX, sourceY) };
            var queue = new Queue<(int x, int y, int entry)>();
            queue.Enqueue((sourceX, sourceY, sourceEntry));
            var seen = new HashSet<(int, int, int)>();
            while (queue.Count > 0)
            {
                var (x, y, entry) = queue.Dequeue();
                if (!seen.Add((x, y, entry))) continue;
                if (x < 0 || y < 0 || x >= width || y >= height) continue;
                var c = m_Cells[x, y];
                if (c == null) continue;
                int mask = c.Mask;
                if ((mask & entry) == 0) { r.stopCell = (x, y); continue; } // blocked: spills at this cell's edge
                r.wet.Add((x, y));
                r.wetPorts.TryGetValue((x, y), out int ports);
                ports |= entry;
                if (c.cistern) { r.solved = true; r.wetPorts[(x, y)] = ports; continue; }
                foreach (var d in new[] { N, E, S, W })
                {
                    if (d == entry || (mask & d) == 0) continue;
                    ports |= d;
                    int nx = x + (d == E ? 1 : d == W ? -1 : 0);
                    int ny = y + (d == S ? 1 : d == N ? -1 : 0);
                    r.stopCell = (x, y);
                    queue.Enqueue((nx, ny, Opposite(d)));
                }
                r.wetPorts[(x, y)] = ports;
            }
            return r;
        }

        public bool AllGatesSolved()
        {
            foreach (var c in m_Cells)
                if (c != null && c.rotatable && c.solvedRotation >= 0 && Rotate(c.baseMask, c.rotation) != Rotate(c.baseMask, c.solvedRotation))
                    return false;
            return true;
        }

        /// <summary>
        /// The Jabal Ithlib layout (3×4). Spring enters (1,0) from the north; the cistern is (2,3).
        /// Gate A (elbow) must send water west, gate B (elbow) east, gate C (straight) west–east.
        /// Wrong turns lead to a dry spill basin or a blocked edge.
        /// </summary>
        public static WaterNetwork CreateIthlib()
        {
            var n = new WaterNetwork(3, 4) { sourceX = 1, sourceY = 0, sourceEntry = N };
            n[1, 0] = new Cell { id = "GateA", baseMask = N | E, rotatable = true, rotation = 0, solvedRotation = 3 };
            n[0, 0] = new Cell { id = "Bend1", baseMask = E | S };
            n[2, 0] = new Cell { id = "SpillBend", baseMask = W | S };
            n[2, 1] = new Cell { id = "SpillBasin", baseMask = N };
            n[0, 1] = new Cell { id = "Run1", baseMask = N | S };
            n[0, 2] = new Cell { id = "GateB", baseMask = N | E, rotatable = true, rotation = 2, solvedRotation = 0 };
            n[1, 2] = new Cell { id = "GateC", baseMask = N | S, rotatable = true, rotation = 0, solvedRotation = 1 };
            n[2, 2] = new Cell { id = "Bend2", baseMask = W | S };
            n[2, 3] = new Cell { id = "Cistern", baseMask = N, cistern = true };
            return n;
        }
    }
}
