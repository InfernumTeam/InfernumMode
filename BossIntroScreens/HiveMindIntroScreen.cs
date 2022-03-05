using CalamityMod.NPCs.HiveMind;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class HiveMindIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color cursedFlameColor = new Color(0, 175, 51);
            Color corruptFleshColor = new Color(130, 97, 124);
            return Color.Lerp(cursedFlameColor, corruptFleshColor, (float)Math.Sin(completionRatio * MathHelper.Pi * 4f + AnimationCompletion * MathHelper.Pi));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Collective Growth\nThe Hive Mind";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<HiveMind>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}