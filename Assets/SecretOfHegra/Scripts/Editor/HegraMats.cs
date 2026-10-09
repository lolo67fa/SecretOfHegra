using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    /// <summary>The shared material set (one material per surface type, reused everywhere for batching).</summary>
    public class HegraMats
    {
        public Material sand, sandstoneLit, sandstoneShadow, rough, wood, linen, tent, tentStripe, indigo, violet,
            bronze, leather, earth, white, stripeBlue, mantleRed, skinA, skinB, camel, rope, pottery, clothRed,
            clothGreen, clothSaffron, mountain, moon, ember, lanternBody, flame, water, darkInterior, coinGold,
            resin, kohlBlack, clay, rockSandstone, mountainSandstone;
        public Material glowWarm, glowPool, glowFire, glowStar, glowMarker, glowMoon, glowBlue, fade, sky;

        public static HegraMats Create()
        {
            var m = new HegraMats();
            var sandTex = HegraKit.NoiseTexture("T_Sand", new Color(0.62f, 0.45f, 0.36f), new Color(0.78f, 0.58f, 0.45f), 9f, 11);
            var stoneTex = HegraKit.NoiseTexture("T_Sandstone", HegraPalette.SandstoneShadow, HegraPalette.SandstoneLit, 6f, 23, true);
            var roughTex = HegraKit.NoiseTexture("T_RoughStone", new Color(0.45f, 0.27f, 0.22f), new Color(0.75f, 0.5f, 0.4f), 14f, 37);

            m.sand = HegraKit.Lit("M_Sand", Color.white, sandTex, 0.05f);
            m.sandstoneLit = HegraKit.Lit("M_SandstoneLit", Color.white, stoneTex, 0.1f, HegraPalette.SandstoneLit * 0.10f);
            m.sandstoneShadow = HegraKit.Lit("M_SandstoneShadow", new Color(0.78f, 0.66f, 0.62f), stoneTex, 0.08f, HegraPalette.SandstoneShadow * 0.07f);
            m.rough = HegraKit.Lit("M_RoughStone", Color.white, roughTex, 0.05f);
            m.wood = HegraKit.Lit("M_Wood", HegraPalette.Leather * 1.1f, null, 0.15f);
            m.linen = HegraKit.Lit("M_Linen", HegraPalette.Linen, null, 0.1f);
            m.tent = HegraKit.Lit("M_TentCloth", new Color(0.55f, 0.42f, 0.33f), null, 0.05f);
            m.tentStripe = HegraKit.Lit("M_TentStripe", new Color(0.35f, 0.16f, 0.14f), null, 0.05f);
            m.indigo = HegraKit.Lit("M_Indigo", HegraPalette.Indigo, null, 0.15f);
            m.violet = HegraKit.Lit("M_Violet", HegraPalette.Violet, null, 0.2f);
            m.bronze = HegraKit.Lit("M_Bronze", HegraPalette.Bronze, null, 0.6f, HegraPalette.Bronze * 0.15f);
            m.leather = HegraKit.Lit("M_Leather", HegraPalette.Leather, null, 0.2f);
            m.earth = HegraKit.Lit("M_EarthCloth", HegraPalette.Earth, null, 0.05f);
            m.white = HegraKit.Lit("M_WhiteTunic", new Color(0.92f, 0.9f, 0.85f), null, 0.1f);
            m.stripeBlue = HegraKit.Lit("M_StripeBlue", HegraPalette.StripeBlue, null, 0.1f);
            m.mantleRed = HegraKit.Lit("M_MantleRed", HegraPalette.MantleRed, null, 0.1f);
            m.skinA = HegraKit.Lit("M_SkinA", new Color(0.62f, 0.43f, 0.32f), null, 0.25f);
            m.skinB = HegraKit.Lit("M_SkinB", new Color(0.5f, 0.34f, 0.25f), null, 0.25f);
            m.camel = HegraKit.Lit("M_CamelFur", new Color(0.66f, 0.5f, 0.34f), null, 0.05f);
            m.rope = HegraKit.Lit("M_Rope", new Color(0.55f, 0.46f, 0.32f), null, 0.05f);
            m.pottery = HegraKit.Lit("M_Pottery", new Color(0.72f, 0.42f, 0.28f), null, 0.15f);
            m.clay = HegraKit.Lit("M_Clay", new Color(0.78f, 0.52f, 0.38f), null, 0.12f);
            m.clothRed = HegraKit.Lit("M_ClothRed", new Color(0.6f, 0.18f, 0.16f), null, 0.05f);
            m.clothGreen = HegraKit.Lit("M_ClothGreen", new Color(0.24f, 0.36f, 0.26f), null, 0.05f);
            m.clothSaffron = HegraKit.Lit("M_ClothSaffron", new Color(0.82f, 0.58f, 0.2f), null, 0.05f);
            m.coinGold = HegraKit.Lit("M_CoinMetal", new Color(0.85f, 0.7f, 0.42f), null, 0.75f, new Color(0.2f, 0.15f, 0.05f));
            m.resin = HegraKit.Lit("M_Frankincense", new Color(0.95f, 0.85f, 0.6f), null, 0.6f, new Color(0.15f, 0.12f, 0.05f));
            m.kohlBlack = HegraKit.Lit("M_Kohl", HegraPalette.Kohl, null, 0.3f);
            m.mountain = HegraKit.Unlit("M_MountainSilhouette", new Color(0.05f, 0.05f, 0.1f));
            // procedural sandstone (SecretOfHegra/Sandstone): palette colours, darker foot, lighter crown, moon rim
            m.rockSandstone = HegraKit.Custom("M_SandstoneRock", "SecretOfHegra/Sandstone");
            m.rockSandstone.SetColor("_BottomColor", HegraPalette.SandstoneShadow * 0.55f);
            m.rockSandstone.SetColor("_TopColor", HegraPalette.SandstoneLit * 0.95f);
            m.rockSandstone.SetColor("_StrataColor", HegraPalette.SandstoneShadow);
            m.rockSandstone.SetColor("_RimColor", HegraPalette.Moonlight);
            m.rockSandstone.SetFloat("_StrataFrequency", 2.2f);
            m.rockSandstone.SetFloat("_StrataStrength", 0.35f);
            m.rockSandstone.SetFloat("_RimStrength", 0.32f);
            m.mountainSandstone = HegraKit.Custom("M_SandstoneMountains", "SecretOfHegra/Sandstone");
            m.mountainSandstone.SetColor("_BottomColor", HegraPalette.SandstoneShadow * 0.3f);
            m.mountainSandstone.SetColor("_TopColor", HegraPalette.SandstoneShadow * 0.75f);
            m.mountainSandstone.SetColor("_StrataColor", HegraPalette.SandstoneShadow);
            m.mountainSandstone.SetColor("_RimColor", HegraPalette.Moonlight);
            m.mountainSandstone.SetFloat("_StrataFrequency", 0.12f);
            m.mountainSandstone.SetFloat("_StrataStrength", 0.25f);
            m.mountainSandstone.SetFloat("_RimStrength", 0.6f);
            m.moon = HegraKit.Unlit("M_Moon", new Color(0.88f, 0.92f, 1f));
            m.ember = HegraKit.Unlit("M_Ember", new Color(1f, 0.45f, 0.12f));
            m.lanternBody = HegraKit.Unlit("M_LanternLit", HegraPalette.Lantern * 1.2f);
            m.flame = HegraKit.Unlit("M_Flame", new Color(1f, 0.75f, 0.3f));
            m.water = HegraKit.Lit("M_WaterStill", HegraPalette.Water * 0.6f, null, 0.9f, HegraPalette.Water * 0.25f);
            m.darkInterior = HegraKit.Lit("M_ChamberStone", new Color(0.45f, 0.3f, 0.26f), null, 0.05f);

            m.glowWarm = HegraKit.Glow("M_GlowLantern", HegraPalette.Lantern, 1.2f, 2.2f);
            m.glowPool = HegraKit.Glow("M_GlowPool", HegraPalette.Lantern, 0.22f, 2.2f);
            m.glowFire = HegraKit.Glow("M_GlowFire", new Color(1f, 0.55f, 0.2f), 1.2f, 1.8f);
            m.glowStar = HegraKit.Glow("M_GlowStar", new Color(0.85f, 0.9f, 1f), 1.4f, 1.2f);
            m.glowMarker = HegraKit.Glow("M_GlowMarker", new Color(0.55f, 0.75f, 1f), 0.9f, 1.4f);
            m.glowMoon = HegraKit.Glow("M_GlowMoon", new Color(0.6f, 0.7f, 1f), 0.8f, 2.5f);
            m.glowBlue = HegraKit.Glow("M_GlowWater", HegraPalette.Water, 1.2f, 1.8f);
            m.fade = HegraKit.Custom("M_Fade", "SecretOfHegra/Fade");
            m.fade.SetColor("_Color", HegraPalette.Night);
            m.sky = HegraKit.Custom("M_NightSky", "SecretOfHegra/NightSky");
            // the Milky Way crosses high over the valley
            m.sky.SetColor("_BandColor", new Color(0.55f, 0.6f, 0.78f));
            m.sky.SetFloat("_BandIntensity", 0.4f);
            m.sky.SetVector("_BandNormal", new Vector4(0.78f, 0.2f, -0.59f, 0f));
            return m;
        }
    }
}
