using System;

namespace SecretOfHegra
{
    /// <summary>
    /// Ordered-selection rule for the Elephant Rock inscription: the player repeats a short sequence of
    /// glyph indices. A wrong pick only restarts the attempt (nothing is lost). Pure C# for edit-mode tests.
    /// </summary>
    public class GlyphSequence
    {
        public enum Result { Ignored, Progress, Wrong, Solved }

        readonly int[] m_Order;

        public GlyphSequence(params int[] order)
        {
            if (order == null || order.Length == 0) throw new ArgumentException("Sequence needs at least one glyph.");
            m_Order = (int[])order.Clone();
        }

        public int Length => m_Order.Length;
        public int Progress { get; private set; }
        public int Mistakes { get; private set; }
        public bool Solved { get; private set; }

        /// <summary>The glyph the player should pick next (-1 once solved).</summary>
        public int Expected => Solved ? -1 : m_Order[Progress];

        public int this[int i] => m_Order[i];

        public Result Select(int glyph)
        {
            if (Solved) return Result.Ignored;
            if (glyph == m_Order[Progress])
            {
                Progress++;
                if (Progress < m_Order.Length) return Result.Progress;
                Solved = true;
                return Result.Solved;
            }
            Progress = 0;
            Mistakes++;
            return Result.Wrong;
        }

        /// <summary>Starts a fresh attempt (mistake count is kept so hints can escalate).</summary>
        public void Restart() => Progress = 0;

        public void MarkSolved()
        {
            Solved = true;
            Progress = m_Order.Length;
        }
    }
}
