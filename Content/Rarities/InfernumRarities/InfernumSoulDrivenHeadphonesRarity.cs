using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumSoulDrivenHeadphonesRarity : ModRarity
    {
        public override Color RarityColor => new(245, 151, 208);

        internal static List<RaritySparkle> MusicSymbols = new();

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            Color outerColor = Main.hslToRgb(Main.GlobalTimeWrappedHourly % 1f, 1f, 0.6f) * 0.2f;
            outerColor = Color.Lerp(outerColor, Color.HotPink, 0.3f);
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: Color.MediumPurple, textOuterColor: outerColor, Color.Lerp(Color.Black, outerColor, 0.2f));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref MusicSymbols, 8, SparkleType.MusicNotes);
        }
    }
}
