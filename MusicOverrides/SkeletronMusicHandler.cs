using CalamityMod.Events;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.MusicOverrides
{
    public class SkeletronMusicHandler : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/Boss3");

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(NPCID.SkeletronHead) && InfernumMode.CanUseCustomAIs && !BossRushEvent.BossRushActive;

        public override SceneEffectPriority Priority => SceneEffectPriority.BossLow;
    }
}
