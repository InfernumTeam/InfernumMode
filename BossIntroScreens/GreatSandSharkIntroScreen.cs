using CalamityMod.NPCs.GreatSandShark;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class GreatSandSharkIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color sandColor = Color.Lerp(new Color(191, 143, 103), new Color(107, 64, 73), (float)Math.Sin(AnimationCompletion * MathHelper.TwoPi + completionRatio * MathHelper.Pi) * 0.5f + 0.5f);
            float cyanColorInterpolant = Utils.InverseLerp(0.77f, 1f, (float)Math.Sin(AnimationCompletion * -MathHelper.TwoPi + completionRatio * MathHelper.TwoPi) * 0.5f + 0.5f);
            return Color.Lerp(sandColor, new Color(19, 255, 203), cyanColorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Apex Predator\nThe Great Sand Shark";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<GreatSandShark>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => InfernumMode.CalamityMod.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/GreatSandSharkRoar");
    }
}