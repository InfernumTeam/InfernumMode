using CalamityMod;
using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class BereftVassalIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color sandColor = new(191, 142, 104);
            Color waterColor = new(32, 175, 188);
            return Color.Lerp(sandColor, waterColor, Sin(completionRatio * Pi * 4f + AnimationCompletion * Pi));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive()
        {
            int bereftVassalIndex = NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>());

            if (bereftVassalIndex == -1)
                return false;

            NPC bereftVassal = Main.npc[bereftVassalIndex];
            return bereftVassal.ModNPC<BereftVassal>().CurrentAttack != BereftVassal.BereftVassalAttackType.IdleState && InfernumMode.CanUseCustomAIs;
        }

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
