using CalamityMod.NPCs.Leviathan;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class LeviathanIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            float colorFadeInterpolant = (float)Math.Sin(AnimationCompletion * MathHelper.TwoPi + completionRatio * MathHelper.Pi * 64f) * 0.5f + 0.5f;
            Color lightSkinColor = new Color(80, 211, 174);
            Color darkSkinColor = new Color(0, 149, 159);
            return Color.Lerp(lightSkinColor, darkSkinColor, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Timeworn Beast\nThe Leviathan";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Leviathan>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => InfernumMode.CalamityMod.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/LeviathanRoarCharge");
    }
}