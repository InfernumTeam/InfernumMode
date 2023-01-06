using CalamityMod.NPCs.Signus;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class SignusIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            return Color.Lerp(Color.Violet, Color.Black, 0.67f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "The Fathomless Assassin\nSignus";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Signus>());

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}