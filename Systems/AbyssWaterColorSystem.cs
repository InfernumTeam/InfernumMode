using CalamityMod;
using CalamityMod.ILEditing;
using InfernumMode.WorldGeneration;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class AbyssWaterColorSystem : ModSystem
    {
        public static float WaterBlacknessInterpolant
        {
            get;
            set;
        } = 1f;

        internal VertexColors ChangeAbyssColors(VertexColors initialColor, int liquidType, Point p)
        {
            bool isAbyssWater = liquidType == ModContent.Find<ModWaterStyle>("InfernumMode/AbyssWater").Slot;
            if (WorldSaveSystem.InPostAEWUpdateWorld && isAbyssWater)
            {
                Color acidWaterColor = new(62, 217, 145);
                Color abyssWaterColor = new(29, 15, 56);

                float topLeftBrightness = initialColor.TopLeftColor.ToVector3().Length() / 1.732f;
                float topRightBrightness = initialColor.TopRightColor.ToVector3().Length() / 1.732f;
                float bottomLeftBrightness = initialColor.BottomLeftColor.ToVector3().Length() / 1.732f;
                float bottomRightBrightness = initialColor.BottomRightColor.ToVector3().Length() / 1.732f;

                float blacknessInterpolant = Utils.GetLerpValue(CustomAbyss.Layer2Top, CustomAbyss.Layer4Top, p.Y, true) * WaterBlacknessInterpolant * 0.44f;
                float sulphuricWaterInterpolant = Utils.GetLerpValue(CustomAbyss.Layer2Top - 24f, CustomAbyss.AbyssTop - 4f, p.Y, true);

                // Conditional exists for optimization purposes.
                if (sulphuricWaterInterpolant <= 0f)
                {
                    initialColor.TopLeftColor = abyssWaterColor * topLeftBrightness;
                    initialColor.TopRightColor = abyssWaterColor * topRightBrightness;
                    initialColor.BottomLeftColor = abyssWaterColor * bottomLeftBrightness;
                    initialColor.BottomRightColor = abyssWaterColor * bottomRightBrightness;
                }
                else
                {
                    initialColor.TopLeftColor = Color.Lerp(abyssWaterColor, acidWaterColor, sulphuricWaterInterpolant) * topLeftBrightness;
                    initialColor.TopRightColor = Color.Lerp(abyssWaterColor, acidWaterColor, sulphuricWaterInterpolant) * topRightBrightness;
                    initialColor.BottomLeftColor = Color.Lerp(abyssWaterColor, acidWaterColor, sulphuricWaterInterpolant) * bottomLeftBrightness;
                    initialColor.BottomRightColor = Color.Lerp(abyssWaterColor, acidWaterColor, sulphuricWaterInterpolant) * bottomRightBrightness;
                }

                if (blacknessInterpolant > 0f)
                {
                    initialColor.TopLeftColor = Color.Lerp(initialColor.TopLeftColor, Color.Black, blacknessInterpolant);
                    initialColor.TopRightColor = Color.Lerp(initialColor.TopRightColor, Color.Black, blacknessInterpolant);
                    initialColor.BottomLeftColor = Color.Lerp(initialColor.BottomLeftColor, Color.Black, blacknessInterpolant);
                    initialColor.BottomRightColor = Color.Lerp(initialColor.BottomRightColor, Color.Black, blacknessInterpolant);
                }
            }

            return initialColor;
        }

        public override void Load()
        {
            ILChanges.ExtraColorChangeConditions += ChangeAbyssColors;
        }

        public override void Unload()
        {
            ILChanges.ExtraColorChangeConditions -= ChangeAbyssColors;
        }

        public override void PreUpdateEntities()
        {
            WaterBlacknessInterpolant = MathHelper.Clamp(WaterBlacknessInterpolant + 0.02f, 0f, 1f);
        }
    }
}