using CalamityMod;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumOceanFlowerRarity : ModRarity
    {
        public override Color RarityColor => Color.Cyan;

        internal static List<RaritySparkle> FlowerRaritySparkles = new();

        public static Texture2D BubbleTexture => ModContent.Request<Texture2D>("CalamityMod/Particles/Bubble").Value;

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, Color.LightSkyBlue, CalamityUtils.ColorSwap(Color.SkyBlue, Color.DeepSkyBlue, 2), new(12, 26, 47));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref FlowerRaritySparkles, 6, SparkleType.BubbleSparkle);
        }
    }
}
