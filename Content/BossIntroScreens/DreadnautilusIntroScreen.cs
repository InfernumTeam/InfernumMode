using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens
{
    public class DreadnautilusIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float limeColorInterpolant = Utils.GetLerpValue(0.77f, 1f, MathF.Sin(AnimationCompletion * -MathHelper.Pi * 4f + completionRatio * MathHelper.Pi) * 0.5f + 0.5f);
            Color skinColor = new(216, 93, 82);
            Color bloodColor = new(193, 45, 6);
            return Color.Lerp(skinColor, bloodColor, limeColorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Nightmare of the Blood Moon\nDreadnautilus";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.BloodNautilus);

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}