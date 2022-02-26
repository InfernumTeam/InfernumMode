using CalamityMod.NPCs.Cryogen;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class CryogenIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            return Color.Lerp(Color.Cyan, Color.LightCyan, ((float)Math.Sin(AnimationCompletion * MathHelper.Pi * 4f) * 0.5f + 0.5f) * 0.72f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "The Unstable Prison\nCryogen";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Cryogen>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}