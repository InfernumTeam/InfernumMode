using CalamityMod.NPCs.DesertScourge;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DesertScourge;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class DesertScourgeIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorFadeInterpolant = Sin(AnimationCompletion * TwoPi) * 0.5f + 0.5f;
            return Color.Lerp(new Color(229, 197, 146), new Color(119, 76, 38), colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Dried Glutton\nThe Desert Scourge";

        public override bool ShouldBeActive()
        {
            int desertScourgeIndex = NPC.FindFirstNPC(ModContent.NPCType<DesertScourgeHead>());
            return desertScourgeIndex >= 0 && (Main.npc[desertScourgeIndex].ai[0] != (int)DesertScourgeHeadBigBehaviorOverride.DesertScourgeAttackType.SpawnAnimation || Main.npc[desertScourgeIndex].Infernum().ExtraAI[0] >= 1f);
        }

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}