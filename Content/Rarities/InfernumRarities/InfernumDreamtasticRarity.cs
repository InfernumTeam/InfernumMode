using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumDreamtasticRarity : ModRarity
    {
        public override Color RarityColor => new(75, 38, 158);

        internal static List<RaritySparkle> BookSymbols = new();

        public static Texture2D SymbolTexture => ModContent.Request<Texture2D>("InfernumMode/Content/Rarities/Textures/Book").Value;

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            Color outerColor = new(141, 45, 122);
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: Color.MediumPurple, textOuterColor: outerColor, Color.Lerp(Color.Black, outerColor, 0.2f));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref BookSymbols, 16, SparkleType.BookSparkle);
        }
    }
}
