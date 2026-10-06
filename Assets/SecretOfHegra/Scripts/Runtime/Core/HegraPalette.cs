using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>Art-direction palette (stylized realism: cold moonlight, warm lanterns, pink sandstone).</summary>
    public static class HegraPalette
    {
        public static readonly Color Night = Hex(0x0B0F1E);
        public static readonly Color Kohl = Hex(0x2E2440);
        public static readonly Color SandstoneShadow = Hex(0x9B5A45);
        public static readonly Color SandstoneLit = Hex(0xD99673);
        public static readonly Color Lantern = Hex(0xEBB34E);
        public static readonly Color Linen = Hex(0xEDE1CC);

        public static readonly Color Indigo = Hex(0x2B3A78);
        public static readonly Color Violet = Hex(0x6A3F8F);
        public static readonly Color Bronze = Hex(0xB07A3A);
        public static readonly Color Leather = Hex(0x6B4428);
        public static readonly Color Earth = Hex(0x7A5A3A);
        public static readonly Color MantleRed = Hex(0x9C2F2A);
        public static readonly Color StripeBlue = Hex(0x2F5E9C);
        public static readonly Color Moonlight = Hex(0x9FB7E8);
        public static readonly Color Water = Hex(0x4FA3C7);

        public static readonly Color PanelBg = new Color(0.07f, 0.08f, 0.15f, 0.92f);
        public static readonly Color ButtonBg = new Color(0.18f, 0.14f, 0.25f, 1f);
        public static readonly Color ButtonHighlight = new Color(0.42f, 0.28f, 0.18f, 1f);
        public static readonly Color Text = Linen;

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }
    }
}
