using CalamityMod.NPCs.DevourerofGods;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
	public class DoGIntroScreen : BaseIntroScreen
    {
        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => true;

        public override string TextToDisplay => "The Prideful, Devourer of Gods";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>());

        public override LegacySoundStyle SoundToPlayWithText => InfernumMode.Instance.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/DoGAttack");
    }
}