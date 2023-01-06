using CalamityMod.NPCs.ProfanedGuardians;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class ProfanedGuardiansIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.Lerp(Color.Orange, Color.Yellow, 0.65f);

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override int AnimationTime => 180;

        public override string TextToDisplay => "Disciples of Purity\nThe Profaned Guardians";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>());

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("CalamityMod/Sounds/Custom/ProvidenceSpawn");
    }
}