using CalamityMod.NPCs.ProfanedGuardians;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class ProfanedGuardiansIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.Lerp(Color.Orange, Color.Yellow, 0.65f);

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override int AnimationTime => 180;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceSpawn");
    }
}
