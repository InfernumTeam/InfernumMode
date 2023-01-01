using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

// In the base namespace for convience with how often textures are used with newer visuals.
// Please maintain Type -> Alphabetical order.
namespace InfernumMode
{
    public static class InfernumTextureRegistry
    {
        public static readonly Asset<Texture2D> BinaryLine = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Trails/BinaryLine");

        public static readonly Asset<Texture2D> BloomLine = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Lines/BloomLine");

        public static readonly Asset<Texture2D> BloomLineSmall = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Lines/BloomLineSmall");

        public static readonly Asset<Texture2D> Cloud = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/NebulaGas1");

        public static readonly Asset<Texture2D> Cloud2 = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/NebulaGas2");

        public static readonly Asset<Texture2D> CultistRayMap = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleGradients/CultistRayMap");

        public static readonly Asset<Texture2D> DiagonalGleam = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleGradients/DiagonalGleam");

        public static readonly Asset<Texture2D> DistortedBloomRing = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/DistortedBloomRing");

        public static readonly Asset<Texture2D> EmpressStar = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/EmpressStar");

        public static readonly Asset<Texture2D> Gleam = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/Gleam");

        public static readonly Asset<Texture2D> GrayscaleWater = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/ScrollingLayers/GrayscaleWater");

        public static readonly Asset<Texture2D> HollowCircleSoftEdge = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/HollowCircleSoftEdge");

        public static readonly Asset<Texture2D> Invisible = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Invisible");

        public static readonly Asset<Texture2D> LargeStar = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/LargeStar");

        public static readonly Asset<Texture2D> LaserCircle = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/LaserCircle");

        public static readonly Asset<Texture2D> LightningStreak = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Trails/StreakLightning");

        public static readonly Asset<Texture2D> Line = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Lines/Line");

        public static readonly Asset<Texture2D> Pixel = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Pixel");

        public static readonly Asset<Texture2D> Shadow = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/ScrollingLayers/Shadow1");

        public static readonly Asset<Texture2D> Shadow2 = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/ScrollingLayers/Shadow2");

        public static readonly Asset<Texture2D> Smoke = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/Smoke");

        public static readonly Asset<Texture2D> Stars = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/ScrollingLayers/Stars");

        public static readonly Asset<Texture2D> StreakBubble = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Trails/Streak3");

        public static readonly Asset<Texture2D> StreakBubbleGlow = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Trails/Streak4");

        public static readonly Asset<Texture2D> StreakFaded = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Trails/StreakFaded");

        public static readonly Asset<Texture2D> StreakFire = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Trails/StreakFire");

        public static readonly Asset<Texture2D> StreakSolid = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Trails/StreakSolid");

        public static readonly Asset<Texture2D> StreakThickGlow = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Trails/Streak2");

        public static readonly Asset<Texture2D> StreakThinGlow = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Trails/Streak1");

        public static readonly Asset<Texture2D> TelegraphLine = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/LaserWallTelegraphBeam");

        public static readonly Asset<Texture2D> VoronoiShapes = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleGradients/VoronoiShapes");

        public static readonly Asset<Texture2D> Water = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/ScrollingLayers/Water");

        public static readonly Asset<Texture2D> WhiteHole = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GreyscaleObjects/WhiteHole");

        public static readonly string InvisPath = "InfernumMode/ExtraTextures/Invisible";
    }
}
