using CalamityMod;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumSoulDrivenHeadphonesRarity : ModRarity
    {
        public override Color RarityColor => new(245, 151, 208);

        internal static List<RaritySparkle> CodeSymbols = new();

        public static Texture2D SymbolTexture => ModContent.Request<Texture2D>("InfernumMode/Content/Rarities/Textures/MusicNoteTextures").Value;

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            Color outerColor = Main.hslToRgb(Main.GlobalTimeWrappedHourly % 1f, 1f, 0.6f) * 0.2f;
            outerColor = Color.Lerp(outerColor, Color.HotPink, 0.3f);
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: Color.MediumPurple, textOuterColor: outerColor, Color.Lerp(Color.Black, outerColor, 0.2f));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref CodeSymbols, 8, SparkleType.MusicNotes);
        }
    }
}
