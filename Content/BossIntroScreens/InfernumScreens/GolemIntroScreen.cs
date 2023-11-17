using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class GolemIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color stoneColor = new(130, 68, 8);
            float sunColorInterpolant = Utils.GetLerpValue(0.77f, 1f, Sin(AnimationCompletion * Pi * -4f + completionRatio * TwoPi) * 0.5f + 0.5f);
            return Color.Lerp(stoneColor, new Color(255, 170, 0), sunColorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override LocalizedText TextToDisplay => GetLocalizedText(IntroScreenManager.ShouldDisplayJokeIntroText ? "JokeTextToDisplay" : "TextToDisplay");

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.Golem) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
