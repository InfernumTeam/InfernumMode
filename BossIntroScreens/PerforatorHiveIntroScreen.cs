using CalamityMod.NPCs.Perforator;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class PerforatorHiveIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color ichorColor = new Color(252, 180, 3);
            return Color.Lerp(Color.DarkRed, ichorColor, ((float)Math.Sin(completionRatio * MathHelper.Pi * 3f + AnimationCompletion * MathHelper.PiOver2) * 0.5f + 0.5f) * 0.6f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Bloodied Parasites\nThe Perforators";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<PerforatorHive>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}