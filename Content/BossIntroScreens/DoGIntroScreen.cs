using CalamityMod.NPCs.DevourerofGods;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class DoGIntroScreen : BaseIntroScreen
    {
        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => true;

        public override float TextScale => MajorBossTextScale;

        public override TextColorData TextColor => new(c => Color.Lerp(Color.Cyan, Color.Fuchsia, (float)Math.Sin(AnimationCompletion * 8f + c * MathHelper.Pi * 3f) * 0.5f + 0.5f));

        public override Color ScreenCoverColor => Color.Black;

        public override string TextToDisplay => "The Conceited\nDevourer of Gods";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>());

        public override SoundStyle? SoundToPlayWithTextCreation => DevourerofGodsHead.AttackSound;
    }
}