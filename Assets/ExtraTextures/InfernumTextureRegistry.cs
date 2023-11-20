using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

// This is in the base namespace for convience with how often textures are used with newer visuals.
// Please maintain Type -> Alphabetical order when adding new textures to the registry.
namespace InfernumMode.Assets.ExtraTextures
{
    public class InfernumTextureRegistry : ModSystem
    {
        #region Paths
        public static string InvisPath => "InfernumMode/Assets/ExtraTextures/Invisible";
        #endregion

        #region Textures
        public static Asset<Texture2D> Arrow { get; private set; }

        public static Asset<Texture2D> BigGreyscaleCircle { get; private set; }

        public static Asset<Texture2D> BinaryLine { get; private set; }

        public static Asset<Texture2D> BloomCircle { get; private set; }

        public static Asset<Texture2D> BloomFlare { get; private set; }

        public static Asset<Texture2D> BloomLine { get; private set; }

        public static Asset<Texture2D> BloomLineSmall { get; private set; }

        public static Asset<Texture2D> BlurryPerlinNoise { get; private set; }

        public static Asset<Texture2D> Bubble { get; private set; }

        public static Asset<Texture2D> Cloud { get; private set; }

        public static Asset<Texture2D> Cloud2 { get; private set; }

        public static Asset<Texture2D> CrispCircle { get; private set; }

        public static Asset<Texture2D> CrustyNoise { get; private set; }

        public static Asset<Texture2D> CracksNoise { get; private set; }

        public static Asset<Texture2D> CrystalNoise { get; private set; }

        public static Asset<Texture2D> CrystalNoiseNormal { get; private set; }

        public static Asset<Texture2D> CultistRayMap { get; private set; }

        public static Asset<Texture2D> DayGradient { get; private set; }

        public static Asset<Texture2D> DiagonalGleam { get; private set; }

        public static Asset<Texture2D> DistortedBloomRing { get; private set; }

        public static Asset<Texture2D> DistortedCircle { get; private set; }

        public static Asset<Texture2D> EmpressStar { get; private set; }

        public static Asset<Texture2D> FireNoise { get; private set; }

        public static Asset<Texture2D> Gleam { get; private set; }

        public static Asset<Texture2D> GreyscalePill { get; private set; }

        public static Asset<Texture2D> GrayscaleWater { get; private set; }

        public static Asset<Texture2D> HarshNoise { get; private set; }

        public static Asset<Texture2D> HexagonGrid { get; private set; }

        public static Asset<Texture2D> HollowCircleSoftEdge { get; private set; }

        public static Asset<Texture2D> HoneycombNoise { get; private set; }

        public static Asset<Texture2D> HolyCrystalLayer { get; private set; }

        public static Asset<Texture2D> HolyFireLayer { get; private set; }

        public static Asset<Texture2D> HolyFirePixelLayer { get; private set; }

        public static Asset<Texture2D> HolyFirePixelLayerNight { get; private set; }

        public static Asset<Texture2D> HyperplaneMatrixCode { get; private set; }

        public static Asset<Texture2D> Invisible { get; private set; }

        public static Asset<Texture2D> LargeStar { get; private set; }

        public static Asset<Texture2D> LaserCircle { get; private set; }

        public static Asset<Texture2D> LavaNoise { get; private set; }

        public static Asset<Texture2D> LessCrustyNoise { get; private set; }

        public static Asset<Texture2D> LightningStreak { get; private set; }

        public static Asset<Texture2D> Line { get; private set; }

        public static Asset<Texture2D> MilkyNoise { get; private set; }

        public static Asset<Texture2D> MoonLordBackground { get; private set; }

        public static Asset<Texture2D> NightGradient { get; private set; }

        public static Asset<Texture2D> Pixel { get; private set; }

        public static Asset<Texture2D> SimpleNoise { get; private set; }

        public static Asset<Texture2D> Shadow { get; private set; }

        public static Asset<Texture2D> Shadow2 { get; private set; }

        public static Asset<Texture2D> Smoke { get; private set; }

        public static Asset<Texture2D> SmokyNoise { get; private set; }

        public static Asset<Texture2D> SolidEdgeGradient { get; private set; }

        public static Asset<Texture2D> Smudges { get; private set; }

        public static Asset<Texture2D> SquareSmoke { get; private set; }

        public static Asset<Texture2D> Stars { get; private set; }

        public static Asset<Texture2D> StreakBigBackground { get; private set; }

        public static Asset<Texture2D> StreakBigInner { get; private set; }

        public static Asset<Texture2D> StreakBubble { get; private set; }

        public static Asset<Texture2D> StreakBubbleGlow { get; private set; }

        public static Asset<Texture2D> StreakFaded { get; private set; }

        public static Asset<Texture2D> StreakFire { get; private set; }

        public static Asset<Texture2D> StreakGeneric { get; private set; }

        public static Asset<Texture2D> StreakLightning { get; private set; }

        public static Asset<Texture2D> StreakMagma { get; private set; }

        public static Asset<Texture2D> StreakSolid { get; private set; }

        public static Asset<Texture2D> StreakThickGlow { get; private set; }

        public static Asset<Texture2D> StreakThinGlow { get; private set; }

        public static Asset<Texture2D> TelegraphLine { get; private set; }

        public static Asset<Texture2D> TrypophobiaNoise { get; private set; }

        public static Asset<Texture2D> Void { get; private set; }

        public static Asset<Texture2D> VolcanoWarning { get; private set; }

        public static Asset<Texture2D> VoronoiCelluar { get; private set; }

        public static Asset<Texture2D> VoronoiLoop { get; private set; }

        public static Asset<Texture2D> VoronoiShapes { get; private set; }

        public static Asset<Texture2D> Water { get; private set; }

        public static Asset<Texture2D> WaterNoise { get; private set; }

        public static Asset<Texture2D> WavyNeuronsNoise { get; private set; }

        public static Asset<Texture2D> WavyNoise { get; private set; }

        public static Asset<Texture2D> WhiteHole { get; private set; }
        #endregion

        #region Loading/Unloading
        public override void Load()
        {
            Arrow = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/ArrowBlack", AssetRequestMode.ImmediateLoad);

            BigGreyscaleCircle = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/BigGreyscaleCircle", AssetRequestMode.ImmediateLoad);

            BinaryLine = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/BinaryLine", AssetRequestMode.ImmediateLoad);

            BloomCircle = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle", AssetRequestMode.ImmediateLoad);

            BloomFlare = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/BloomFlare", AssetRequestMode.ImmediateLoad);

            BloomLine = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lines/BloomLine", AssetRequestMode.ImmediateLoad);

            BloomLineSmall = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lines/BloomLineSmall", AssetRequestMode.ImmediateLoad);

            BlurryPerlinNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/BlurryPerlinNoise", AssetRequestMode.ImmediateLoad);

            Bubble = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Bubble", AssetRequestMode.ImmediateLoad);

            Cloud = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/NebulaGas1", AssetRequestMode.ImmediateLoad);

            Cloud2 = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/NebulaGas2", AssetRequestMode.ImmediateLoad);

            CrispCircle = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/CrispCircle", AssetRequestMode.ImmediateLoad);

            CrustyNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/CrustyNoise", AssetRequestMode.ImmediateLoad);

            CracksNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/CracksNoise", AssetRequestMode.ImmediateLoad);

            CrystalNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/Crystals", AssetRequestMode.ImmediateLoad);

            CrystalNoiseNormal = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/CrystalsNormalMap", AssetRequestMode.ImmediateLoad);

            CultistRayMap = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/CultistRayMap", AssetRequestMode.ImmediateLoad);

            DayGradient = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Gradients/DayGradient", AssetRequestMode.ImmediateLoad);

            DiagonalGleam = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/DiagonalGleam", AssetRequestMode.ImmediateLoad);

            DistortedBloomRing = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/DistortedBloomRing", AssetRequestMode.ImmediateLoad);

            DistortedCircle = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/DistortedCircle", AssetRequestMode.ImmediateLoad);

            EmpressStar = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/EmpressStar", AssetRequestMode.ImmediateLoad);

            FireNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/FireNoise", AssetRequestMode.ImmediateLoad);

            Gleam = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam", AssetRequestMode.ImmediateLoad);

            GreyscalePill = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/GreyscalePill", AssetRequestMode.ImmediateLoad);

            GrayscaleWater = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/GrayscaleWater", AssetRequestMode.ImmediateLoad);

            HarshNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/HarshNoise", AssetRequestMode.ImmediateLoad);

            HexagonGrid = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/HexagonGrid", AssetRequestMode.ImmediateLoad);

            HollowCircleSoftEdge = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/HollowCircleSoftEdge", AssetRequestMode.ImmediateLoad);

            HoneycombNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/HoneycombNoise", AssetRequestMode.ImmediateLoad);

            HolyCrystalLayer = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/HolyCrystalLayer", AssetRequestMode.ImmediateLoad);

            HolyFireLayer = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/HolyFireLayer", AssetRequestMode.ImmediateLoad);

            HolyFirePixelLayer = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/HolyFirePixelLayer", AssetRequestMode.ImmediateLoad);

            HolyFirePixelLayerNight = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/HolyFirePixelLayerNight", AssetRequestMode.ImmediateLoad);

            HyperplaneMatrixCode = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/HyperplaneMatrixCode", AssetRequestMode.ImmediateLoad);

            Invisible = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Invisible", AssetRequestMode.ImmediateLoad);

            LargeStar = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/LargeStar", AssetRequestMode.ImmediateLoad);

            LaserCircle = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/LaserCircle", AssetRequestMode.ImmediateLoad);

            LavaNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/LavaNoise", AssetRequestMode.ImmediateLoad);

            LessCrustyNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/LessCrustyNoise", AssetRequestMode.ImmediateLoad);

            LightningStreak = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/StreakLightning", AssetRequestMode.ImmediateLoad);

            Line = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lines/Line", AssetRequestMode.ImmediateLoad);

            MilkyNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/MilkyNoise", AssetRequestMode.ImmediateLoad);

            MoonLordBackground = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/MoonLordBGLayer", AssetRequestMode.ImmediateLoad);

            NightGradient = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Gradients/NightGradient", AssetRequestMode.ImmediateLoad);

            Pixel = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Pixel", AssetRequestMode.ImmediateLoad);

            SimpleNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/SimpleNoise", AssetRequestMode.ImmediateLoad);

            Shadow = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/Shadow1", AssetRequestMode.ImmediateLoad);

            Shadow2 = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/Shadow2", AssetRequestMode.ImmediateLoad);

            Smoke = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Smoke", AssetRequestMode.ImmediateLoad);

            SmokyNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/SmokyNoise", AssetRequestMode.ImmediateLoad);

            SolidEdgeGradient = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/SolidEdgeGradient", AssetRequestMode.ImmediateLoad);

            Smudges = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Smudges", AssetRequestMode.ImmediateLoad);

            SquareSmoke = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/SquareSmoke", AssetRequestMode.ImmediateLoad);

            Stars = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/Stars", AssetRequestMode.ImmediateLoad);

            StreakBigBackground = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/MegaStreakBacking", AssetRequestMode.ImmediateLoad);

            StreakBigInner = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/MegaStreakInner", AssetRequestMode.ImmediateLoad);

            StreakBubble = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/Streak3", AssetRequestMode.ImmediateLoad);

            StreakBubbleGlow = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/Streak4", AssetRequestMode.ImmediateLoad);

            StreakFaded = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/StreakFaded", AssetRequestMode.ImmediateLoad);

            StreakFire = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/StreakFire", AssetRequestMode.ImmediateLoad);

            StreakGeneric = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/GenericStreak", AssetRequestMode.ImmediateLoad);

            StreakLightning = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ZapTrail", AssetRequestMode.ImmediateLoad);

            StreakMagma = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/StreakMagma", AssetRequestMode.ImmediateLoad);

            StreakSolid = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/StreakSolid", AssetRequestMode.ImmediateLoad);

            StreakThickGlow = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/Streak2", AssetRequestMode.ImmediateLoad);

            StreakThinGlow = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Trails/Streak1", AssetRequestMode.ImmediateLoad);

            TelegraphLine = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/LaserWallTelegraphBeam", AssetRequestMode.ImmediateLoad);

            TrypophobiaNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/TrypophobiaNoise", AssetRequestMode.ImmediateLoad);

            Void = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/Void", AssetRequestMode.ImmediateLoad);

            VolcanoWarning = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/VolcanoWarningBlack", AssetRequestMode.ImmediateLoad);

            VoronoiCelluar = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/VoronoiCellular", AssetRequestMode.ImmediateLoad);

            VoronoiLoop = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/VoronoiLoop", AssetRequestMode.ImmediateLoad);

            VoronoiShapes = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/VoronoiShapes", AssetRequestMode.ImmediateLoad);

            Water = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/Water", AssetRequestMode.ImmediateLoad);

            WaterNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/WaterNoise", AssetRequestMode.ImmediateLoad);

            WavyNeuronsNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/WavyNeurons", AssetRequestMode.ImmediateLoad);

            WavyNoise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/WavyNoise", AssetRequestMode.ImmediateLoad);

            WhiteHole = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/WhiteHole", AssetRequestMode.ImmediateLoad);
        }

        public override void Unload()
        {
            Arrow = null;

            BigGreyscaleCircle = null;

            BinaryLine = null;

            BloomCircle = null;

            BloomFlare = null;

            BloomLine = null;

            BloomLineSmall = null;

            BlurryPerlinNoise = null;

            Bubble = null;

            Cloud = null;

            Cloud2 = null;

            CrispCircle = null;

            CrustyNoise = null;

            CracksNoise = null;

            CrystalNoise = null;

            CrystalNoiseNormal = null;

            CultistRayMap = null;

            DayGradient = null;

            DiagonalGleam = null;

            DistortedBloomRing = null;

            DistortedCircle = null;

            EmpressStar = null;

            FireNoise = null;

            Gleam = null;

            GreyscalePill = null;

            GrayscaleWater = null;

            HarshNoise = null;

            HexagonGrid = null;

            HollowCircleSoftEdge = null;

            HoneycombNoise = null;

            HolyCrystalLayer = null;

            HolyFireLayer = null;

            HolyFirePixelLayer = null;

            HolyFirePixelLayerNight = null;

            HyperplaneMatrixCode = null;

            Invisible = null;

            LargeStar = null;

            LaserCircle = null;

            LavaNoise = null;

            LessCrustyNoise = null;

            LightningStreak = null;

            Line = null;

            MilkyNoise = null;

            MoonLordBackground = null;

            NightGradient = null;

            Pixel = null;

            SimpleNoise = null;

            Shadow = null;

            Shadow2 = null;

            Smoke = null;

            SmokyNoise = null;

            SolidEdgeGradient = null;

            Smudges = null;

            SquareSmoke = null;

            Stars = null;

            StreakBigBackground = null;

            StreakBigInner = null;

            StreakBubble = null;

            StreakBubbleGlow = null;

            StreakFaded = null;

            StreakFire = null;

            StreakGeneric = null;

            StreakLightning = null;

            StreakMagma = null;

            StreakSolid = null;

            StreakThickGlow = null;

            StreakThinGlow = null;

            TelegraphLine = null;

            TrypophobiaNoise = null;

            Void = null;

            VolcanoWarning = null;

            VoronoiCelluar = null;

            VoronoiLoop = null;

            VoronoiShapes = null;

            Water = null;

            WaterNoise = null;

            WavyNeuronsNoise = null;

            WavyNoise = null;

            WhiteHole = null;
        }
        #endregion
    }
}
