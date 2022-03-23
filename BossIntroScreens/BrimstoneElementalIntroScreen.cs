using CalamityMod.NPCs.BrimstoneElemental;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class BrimstoneElementalIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            return Color.Lerp(Color.Red, Color.Orange, ((float)Math.Sin(completionRatio * MathHelper.Pi * 3f + AnimationCompletion * MathHelper.Pi * 5f) * 0.5f + 0.5f) * 0.72f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Scarred Numen\nThe Brimstone Elemental";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<BrimstoneElemental>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => InfernumMode.CalamityMod.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/BrimflameRecharge");
    }
}