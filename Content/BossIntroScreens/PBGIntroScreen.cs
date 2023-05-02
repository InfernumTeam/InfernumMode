using CalamityMod.NPCs.PlaguebringerGoliath;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class PBGIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorFadeInterpolant = MathF.Sin(AnimationCompletion * MathHelper.Pi * 4f + completionRatio * MathHelper.TwoPi) * 0.5f + 0.5f;
            return Color.Lerp(Color.Lime, Color.DarkOliveGreen, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Biomechanical Colossus\nThe Plaguebringer Goliath";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<PlaguebringerGoliath>());

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("CalamityMod/Sounds/Custom/AbilitySounds/PlagueReaperAbility");
    }
}