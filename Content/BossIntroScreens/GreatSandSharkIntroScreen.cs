using CalamityMod.NPCs.GreatSandShark;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class GreatSandSharkIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color sandColor = new(191, 142, 104);
            Color sandColor2 = new(150, 96, 88);
            return Color.Lerp(sandColor, sandColor2, (float)Math.Sin(completionRatio * MathHelper.Pi * 3f - AnimationCompletion * MathHelper.Pi));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Taurus\nThe Great Sand Shark";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<GreatSandShark>());

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}